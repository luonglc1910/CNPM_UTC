using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Nghiệp vụ tồn kho — SCR-A08, SCR-A09, FR-A05, BR-12.
///
/// Nguyên tắc: <see cref="HotelService.StockQuantity"/> và bảng
/// <see cref="InventoryTransaction"/> luôn đi cùng nhau trong **một** lần
/// <c>SaveChangesAsync</c>. Không có đường nào đổi tồn mà không sinh phiếu.
/// Bảng giao dịch chỉ ghi thêm: sai sót sửa bằng phiếu điều chỉnh mới, không sửa bản ghi cũ.
/// </summary>
public interface IInventoryService
{
    Task FillIndexAsync(InventoryIndexViewModel vm, int page);

    Task<InventoryReceiveViewModel> BuildReceiveFormAsync(int? hotelServiceId);
    Task<ServiceResult> ReceiveAsync(InventoryReceiveViewModel form);

    Task<InventoryAdjustViewModel> BuildAdjustFormAsync(int? hotelServiceId);
    Task<ServiceResult> AdjustAsync(InventoryAdjustViewModel form);

    /// <summary>Đổ lại ô chọn dịch vụ khi form phải hiện lại vì có lỗi.</summary>
    Task<IReadOnlyList<SelectListItem>> GetStockedServiceOptionsAsync();

    /// <summary>
    /// Trừ tồn cho một lần bán dịch vụ — BR-12. Chặn khi hết tồn trừ khi bật cả cờ riêng của dịch vụ
    /// lẫn công tắc tổng cho bán âm. Dịch vụ không quản lý kho thì bỏ qua (trả Ok, không sinh phiếu).
    /// KHÔNG gọi SaveChanges — nằm chung transaction với dòng folio bên gọi (SCR-F03, SCR-E02).
    /// </summary>
    Task<ServiceResult> SellAsync(int hotelServiceId, int quantity, int? folioItemId);

    /// <summary>
    /// Hoàn tồn khi hủy một dòng dịch vụ có kho — BR-12, FR-E03. KHÔNG gọi SaveChanges.
    /// </summary>
    Task<ServiceResult> ReturnStockAsync(int hotelServiceId, int quantity, int? folioItemId, string reason);
}

/// <inheritdoc />
public class InventoryService : IInventoryService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly ISettingsReader _settings;

    public InventoryService(HotelDbContext db, IAuditService audit, ISettingsReader settings)
    {
        _db = db;
        _audit = audit;
        _settings = settings;
    }

    // ---------- SCR-A08: bảng tồn + lịch sử ----------

    public async Task FillIndexAsync(InventoryIndexViewModel vm, int page)
    {
        vm.ServiceOptions = await GetStockedServiceOptionsAsync();

        // Đếm trên toàn danh mục đang dùng, không theo bộ lọc — giống banner ở SCR-A06.
        vm.LowStockCount = await _db.HotelServices
            .CountAsync(s => s.IsActive && s.IsStockManaged && s.StockQuantity <= s.MinStockLevel);

        if (vm.IsHistoryTab)
        {
            vm.History = await LoadHistoryAsync(vm, page);
        }
        else
        {
            vm.Stocks = await LoadStocksAsync(vm, page);
        }
    }

    private async Task<PagedList<InventoryStockItemViewModel>> LoadStocksAsync(
        InventoryIndexViewModel vm, int page)
    {
        // Chỉ dịch vụ có quản lý kho mới có mặt ở đây; dịch vụ đã ngừng vẫn hiện để còn đối chiếu
        // số tồn còn lại, nhưng không nhập/điều chỉnh được nữa.
        var query = _db.HotelServices.AsNoTracking().Where(s => s.IsStockManaged);

        if (vm.HotelServiceId is not null)
        {
            query = query.Where(s => s.Id == vm.HotelServiceId);
        }

        if (vm.LowStockOnly)
        {
            query = query.Where(s => s.StockQuantity <= s.MinStockLevel);
        }

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
        {
            var k = vm.Keyword.Trim();
            query = query.Where(s => s.Code.Contains(k) || s.Name.Contains(k));
        }

        var projected = query
            .OrderBy(s => s.Code)
            .Select(s => new InventoryStockItemViewModel
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Unit = s.Unit,
                StockQuantity = s.StockQuantity,
                MinStockLevel = s.MinStockLevel,
                AllowNegativeStock = s.AllowNegativeStock,
                IsActive = s.IsActive,
                LastReceivedAt = s.InventoryTransactions
                    .Where(t => t.Type == InventoryTransactionType.Receive)
                    .Max(t => (DateTime?)t.CreatedAt)
            });

        return await PagedList<InventoryStockItemViewModel>.CreateAsync(projected, page);
    }

    private async Task<PagedList<InventoryTransactionListItemViewModel>> LoadHistoryAsync(
        InventoryIndexViewModel vm, int page)
    {
        var query = _db.InventoryTransactions.AsNoTracking();

        if (vm.HotelServiceId is not null)
        {
            query = query.Where(t => t.HotelServiceId == vm.HotelServiceId);
        }

        if (vm.Type is not null)
        {
            query = query.Where(t => t.Type == vm.Type);
        }

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
        {
            var k = vm.Keyword.Trim();
            query = query.Where(t => t.HotelService.Code.Contains(k) || t.HotelService.Name.Contains(k));
        }

        var projected = query
            // Mới nhất lên đầu: xem lịch sử là để biết vừa có chuyện gì.
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Select(t => new InventoryTransactionListItemViewModel
            {
                CreatedAt = t.CreatedAt,
                Type = t.Type,
                Quantity = t.Quantity,
                StockAfter = t.StockAfter,
                ServiceCode = t.HotelService.Code,
                ServiceName = t.HotelService.Name,
                Unit = t.HotelService.Unit,
                FolioCode = t.FolioItem != null ? t.FolioItem.Folio.Code : null,
                // CreatedBy là Id nhân viên, đổi sang tên ngay trong truy vấn để không phải nạp thêm vòng.
                PerformedBy = _db.Employees
                    .Where(e => e.Id == t.CreatedBy)
                    .Select(e => e.FullName)
                    .FirstOrDefault(),
                Reason = t.Reason
            });

        return await PagedList<InventoryTransactionListItemViewModel>.CreateAsync(projected, page);
    }

    // ---------- SCR-A09: nhập kho ----------

    public async Task<InventoryReceiveViewModel> BuildReceiveFormAsync(int? hotelServiceId)
        => new()
        {
            HotelServiceId = hotelServiceId ?? 0,
            ServiceOptions = await GetStockedServiceOptionsAsync()
        };

    public async Task<ServiceResult> ReceiveAsync(InventoryReceiveViewModel form)
    {
        var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == form.HotelServiceId);

        var check = ValidateStockedService(service);
        if (check is not null)
        {
            return check;
        }

        service!.StockQuantity += form.Quantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            HotelServiceId = service.Id,
            Type = InventoryTransactionType.Receive,
            Quantity = form.Quantity,
            StockAfter = service.StockQuantity,
            Reason = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim()
        });

        _audit.Log("ReceiveInventory", nameof(HotelService), service.Id.ToString(),
            reason: form.Note?.Trim(),
            oldValue: $"{service.StockQuantity - form.Quantity} {service.Unit}",
            newValue: $"{service.StockQuantity} {service.Unit}");

        // Tồn và phiếu nhập lưu cùng một lần: không có cảnh tồn tăng mà thiếu phiếu.
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(message: $"Đã nhập {form.Quantity} {service.Unit} — tồn mới của "
                                + $"{service.Code} là {service.StockQuantity} {service.Unit}.");
    }

    // ---------- SCR-A09: điều chỉnh kho (kiểm kê) ----------

    public async Task<InventoryAdjustViewModel> BuildAdjustFormAsync(int? hotelServiceId)
        => new()
        {
            HotelServiceId = hotelServiceId ?? 0,
            ServiceOptions = await GetStockedServiceOptionsAsync()
        };

    public async Task<ServiceResult> AdjustAsync(InventoryAdjustViewModel form)
    {
        var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == form.HotelServiceId);

        var check = ValidateStockedService(service);
        if (check is not null)
        {
            return check;
        }

        var bookStock = service!.StockQuantity;
        var difference = form.CountedQuantity - bookStock;

        if (difference == 0)
        {
            return ServiceResult.Fail(
                $"Tồn thực tế bằng tồn sổ sách ({bookStock} {service.Unit}), không có chênh lệch để điều chỉnh.",
                nameof(form.CountedQuantity));
        }

        service.StockQuantity = form.CountedQuantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            HotelServiceId = service.Id,
            Type = InventoryTransactionType.Adjust,
            // Ghi phần chênh (+/−), không ghi số đếm được: đọc lịch sử phải thấy ngay lệch bao nhiêu.
            Quantity = difference,
            StockAfter = form.CountedQuantity,
            Reason = form.Reason.Trim()
        });

        _audit.Log("AdjustInventory", nameof(HotelService), service.Id.ToString(),
            reason: form.Reason.Trim(),
            oldValue: $"{bookStock} {service.Unit}",
            newValue: $"{form.CountedQuantity} {service.Unit}");

        await _db.SaveChangesAsync();

        var sign = difference > 0 ? "+" : string.Empty;
        return ServiceResult.Ok(message: $"Đã điều chỉnh {service.Code}: {bookStock} → {form.CountedQuantity} "
                                + $"{service.Unit} (chênh {sign}{difference}).");
    }

    public async Task<IReadOnlyList<SelectListItem>> GetStockedServiceOptionsAsync()
        => await _db.HotelServices.AsNoTracking()
            .Where(s => s.IsActive && s.IsStockManaged)
            .OrderBy(s => s.Code)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                // Kèm luôn tồn hiện tại vào nhãn: người kiểm kê thấy ngay số sổ sách để đối chiếu,
                // khỏi cần JavaScript nạp thêm.
                Text = $"{s.Code} — {s.Name} (tồn {s.StockQuantity} {s.Unit})"
            })
            .ToListAsync();

    // ---------- Bán / hoàn tồn (dùng ở tầng thu ngân & buồng phòng) ----------

    public async Task<ServiceResult> SellAsync(int hotelServiceId, int quantity, int? folioItemId)
    {
        if (quantity <= 0)
        {
            return ServiceResult.Fail("Số lượng bán phải lớn hơn 0.");
        }

        var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == hotelServiceId);
        if (service is null)
        {
            return ServiceResult.Fail("Không tìm thấy dịch vụ.");
        }

        // Dịch vụ không quản lý kho thì không có tồn để trừ, nhưng vẫn bán được bình thường.
        if (!service.IsStockManaged)
        {
            return ServiceResult.Ok();
        }

        if (service.StockQuantity < quantity)
        {
            var allowGlobal = await _settings.GetBoolAsync(SystemSettingKeys.AllowSellWhenOutOfStock);
            if (!(service.AllowNegativeStock && allowGlobal))
            {
                return ServiceResult.Fail(
                    $"Dịch vụ {service.Code} chỉ còn {service.StockQuantity} {service.Unit}, không đủ để bán {quantity}.");
            }
        }

        service.StockQuantity -= quantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            HotelServiceId = service.Id,
            Type = InventoryTransactionType.Sale,
            Quantity = -quantity,
            StockAfter = service.StockQuantity,
            FolioItemId = folioItemId
        });

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReturnStockAsync(int hotelServiceId, int quantity, int? folioItemId, string reason)
    {
        if (quantity <= 0)
        {
            return ServiceResult.Fail("Số lượng hoàn phải lớn hơn 0.");
        }

        var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == hotelServiceId);
        if (service is null)
        {
            return ServiceResult.Fail("Không tìm thấy dịch vụ.");
        }

        if (!service.IsStockManaged)
        {
            return ServiceResult.Ok();
        }

        service.StockQuantity += quantity;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            HotelServiceId = service.Id,
            Type = InventoryTransactionType.Return,
            Quantity = quantity,
            StockAfter = service.StockQuantity,
            FolioItemId = folioItemId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });

        return ServiceResult.Ok();
    }

    /// <summary>Ba điều kiện chung của cả nhập kho lẫn điều chỉnh.</summary>
    private static ServiceResult? ValidateStockedService(HotelService? service)
    {
        if (service is null)
        {
            return ServiceResult.Fail("Không tìm thấy dịch vụ.", nameof(InventoryReceiveViewModel.HotelServiceId));
        }

        if (!service.IsStockManaged)
        {
            return ServiceResult.Fail(
                $"Dịch vụ {service.Code} không bật quản lý kho nên không có tồn để nhập hay điều chỉnh.",
                nameof(InventoryReceiveViewModel.HotelServiceId));
        }

        if (!service.IsActive)
        {
            return ServiceResult.Fail(
                $"Dịch vụ {service.Code} đã ngừng sử dụng. Bật lại dịch vụ trước khi thao tác kho.",
                nameof(InventoryReceiveViewModel.HotelServiceId));
        }

        return null;
    }
}
