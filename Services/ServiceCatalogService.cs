using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Nghiệp vụ danh mục dịch vụ — SCR-A06, SCR-A07, FR-A04, BR-12.
/// Tồn kho nằm ngay trên <see cref="HotelService"/>; màn hình này chỉ bật/tắt việc theo dõi tồn,
/// còn thay đổi số lượng tồn phải đi qua SCR-A08/A09 để luôn có phiếu và lý do.
/// </summary>
public interface IServiceCatalogService
{
    Task<HotelServiceIndexViewModel> SearchAsync(HotelServiceIndexViewModel filter, int page);
    Task<HotelServiceFormViewModel?> GetForEditAsync(int id);
    Task<ServiceResult> CreateAsync(HotelServiceFormViewModel form);
    Task<ServiceResult> UpdateAsync(HotelServiceFormViewModel form);
    Task<ServiceResult> DeactivateAsync(int id);
}

/// <inheritdoc />
public class ServiceCatalogService : IServiceCatalogService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;

    public ServiceCatalogService(HotelDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<HotelServiceIndexViewModel> SearchAsync(HotelServiceIndexViewModel filter, int page)
    {
        var query = _db.HotelServices.AsNoTracking();

        if (!filter.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        if (filter.Category is not null)
        {
            query = query.Where(s => s.Category == filter.Category);
        }

        if (filter.LowStockOnly)
        {
            query = query.Where(s => s.IsStockManaged && s.StockQuantity <= s.MinStockLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.Trim();
            query = query.Where(s => s.Code.Contains(k) || s.Name.Contains(k));
        }

        var projected = query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Code)
            .Select(s => new HotelServiceListItemViewModel
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Category = s.Category,
                UnitPrice = s.UnitPrice,
                Unit = s.Unit,
                IsStockManaged = s.IsStockManaged,
                StockQuantity = s.StockQuantity,
                MinStockLevel = s.MinStockLevel,
                AllowNegativeStock = s.AllowNegativeStock,
                IsActive = s.IsActive
            });

        filter.Results = await PagedList<HotelServiceListItemViewModel>.CreateAsync(projected, page);

        // Đếm trên toàn danh mục đang dùng chứ không theo bộ lọc: đây là con số để cảnh báo,
        // lọc xong mà nó đổi theo thì mất ý nghĩa.
        filter.LowStockCount = await _db.HotelServices
            .CountAsync(s => s.IsActive && s.IsStockManaged && s.StockQuantity <= s.MinStockLevel);

        return filter;
    }

    public async Task<HotelServiceFormViewModel?> GetForEditAsync(int id)
    {
        var entity = await _db.HotelServices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null)
        {
            return null;
        }

        return new HotelServiceFormViewModel
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Category = entity.Category,
            UnitPrice = entity.UnitPrice,
            Unit = entity.Unit,
            IsStockManaged = entity.IsStockManaged,
            MinStockLevel = entity.MinStockLevel,
            AllowNegativeStock = entity.AllowNegativeStock,
            IsActive = entity.IsActive,
            StockQuantity = entity.StockQuantity,
            HasFolioHistory = await _db.FolioItems.AnyAsync(i => i.HotelServiceId == id)
        };
    }

    public async Task<ServiceResult> CreateAsync(HotelServiceFormViewModel form)
    {
        var code = form.Code.Trim().ToUpperInvariant();

        if (await _db.HotelServices.AnyAsync(s => s.Code == code))
        {
            return ServiceResult.Fail($"Mã dịch vụ \"{code}\" đã tồn tại.", nameof(form.Code));
        }

        var entity = new HotelService
        {
            Code = code,
            Name = form.Name.Trim(),
            Category = form.Category,
            UnitPrice = form.UnitPrice,
            Unit = form.Unit.Trim(),
            IsStockManaged = form.IsStockManaged,
            // Dịch vụ mới luôn bắt đầu với tồn 0 — muốn có hàng phải lập phiếu nhập ở SCR-A09.
            StockQuantity = 0,
            MinStockLevel = form.IsStockManaged ? form.MinStockLevel : 0,
            AllowNegativeStock = form.IsStockManaged && form.AllowNegativeStock,
            IsActive = true
        };

        _db.HotelServices.Add(entity);
        await _db.SaveChangesAsync();

        // Ghi log sau khi lưu để có Id thật của bản ghi.
        _audit.Log("CreateHotelService", nameof(HotelService), entity.Id.ToString(),
            newValue: $"{entity.Code} — {entity.Name} — {entity.UnitPrice:N0} ₫/{entity.Unit}");
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(entity.IsStockManaged
            ? "Dịch vụ có quản lý kho nên tồn khởi tạo bằng 0. Cần nhập kho trước khi bán."
            : null);
    }

    public async Task<ServiceResult> UpdateAsync(HotelServiceFormViewModel form)
    {
        var entity = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == form.Id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy dịch vụ cần sửa.");
        }

        // Mã dịch vụ không sửa được sau khi tạo (SCR-A07) — bỏ qua giá trị gửi lên.
        var oldPrice = entity.UnitPrice;
        var wasStockManaged = entity.IsStockManaged;
        var stockBefore = entity.StockQuantity;

        entity.Name = form.Name.Trim();
        entity.Category = form.Category;
        entity.UnitPrice = form.UnitPrice;
        entity.Unit = form.Unit.Trim();
        entity.IsStockManaged = form.IsStockManaged;
        entity.MinStockLevel = form.IsStockManaged ? form.MinStockLevel : 0;
        entity.AllowNegativeStock = form.IsStockManaged && form.AllowNegativeStock;
        entity.IsActive = form.IsActive;

        // BR-11: đổi đơn giá phải ghi lại giá cũ → giá mới. Các dòng đã vào folio không đổi theo
        // vì FolioItem chép đơn giá tại thời điểm ghi nhận.
        if (oldPrice != entity.UnitPrice)
        {
            _audit.Log("ChangeHotelServicePrice", nameof(HotelService), entity.Id.ToString(),
                oldValue: $"{oldPrice:N0} ₫/{entity.Unit}",
                newValue: $"{entity.UnitPrice:N0} ₫/{entity.Unit}");
        }

        string? warning = null;

        if (!wasStockManaged && entity.IsStockManaged)
        {
            // Bật theo dõi kho cho dịch vụ đã tồn tại: tồn khởi tạo = 0 (SCR-A07).
            entity.StockQuantity = 0;

            _audit.Log("EnableStockTracking", nameof(HotelService), entity.Id.ToString(),
                oldValue: "Không quản lý kho", newValue: "Có quản lý kho, tồn khởi tạo 0");

            warning = "Đã bật quản lý kho: tồn khởi tạo bằng 0. Phải nhập kho thì mới bán được dịch vụ này.";
        }
        else if (wasStockManaged && !entity.IsStockManaged)
        {
            _audit.Log("DisableStockTracking", nameof(HotelService), entity.Id.ToString(),
                oldValue: $"Có quản lý kho, tồn {stockBefore}", newValue: "Không quản lý kho");

            warning = "Đã tắt quản lý kho: hệ thống ngừng theo dõi tồn của dịch vụ này. "
                      + "Lịch sử giao dịch kho cũ vẫn được giữ nguyên.";
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Ok(warning);
    }

    public async Task<ServiceResult> DeactivateAsync(int id)
    {
        var entity = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy dịch vụ.");
        }

        if (!entity.IsActive)
        {
            return ServiceResult.Fail("Dịch vụ này đã ngừng sử dụng.");
        }

        // Danh mục đã phát sinh giao dịch thì không xóa cứng — chỉ đánh dấu ngừng sử dụng
        // (nguyên tắc chung nhóm A). Tồn còn lại vẫn giữ nguyên để đối chiếu kho.
        entity.IsActive = false;

        _audit.Log("DeactivateHotelService", nameof(HotelService), entity.Id.ToString(),
            oldValue: "Đang sử dụng", newValue: "Ngừng sử dụng");

        await _db.SaveChangesAsync();

        return ServiceResult.Ok(entity.IsStockManaged && entity.StockQuantity > 0
            ? $"Dịch vụ còn tồn {entity.StockQuantity} {entity.Unit}. Số tồn này vẫn được giữ để đối chiếu kho."
            : null);
    }
}
