using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>Nghiệp vụ danh mục loại phòng — SCR-A01, SCR-A02, FR-A01.</summary>
public interface IRoomTypeService
{
    Task<PagedList<RoomTypeListItemViewModel>> SearchAsync(string? keyword, bool includeInactive, int page);
    Task<RoomTypeFormViewModel?> GetForEditAsync(int id);
    Task<ServiceResult> CreateAsync(RoomTypeFormViewModel form);
    Task<ServiceResult> UpdateAsync(RoomTypeFormViewModel form);
    Task<ServiceResult> DeactivateAsync(int id);

    /// <summary>Loại phòng đang dùng, cho ô chọn ở màn hình phòng.</summary>
    Task<IReadOnlyList<RoomType>> GetActiveAsync();
}

/// <inheritdoc />
public class RoomTypeService : IRoomTypeService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;

    public RoomTypeService(HotelDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedList<RoomTypeListItemViewModel>> SearchAsync(string? keyword, bool includeInactive, int page)
    {
        var query = _db.RoomTypes.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(t => t.Code.Contains(k) || t.Name.Contains(k));
        }

        var projected = query
            .OrderBy(t => t.Code)
            .Select(t => new RoomTypeListItemViewModel
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                StandardCapacity = t.StandardCapacity,
                MaxCapacity = t.MaxCapacity,
                BasePricePerNight = t.BasePricePerNight,
                ExtraGuestFeePerNight = t.ExtraGuestFeePerNight,
                ActiveRoomCount = t.Rooms.Count(r => r.IsActive),
                IsActive = t.IsActive
            });

        return await PagedList<RoomTypeListItemViewModel>.CreateAsync(projected, page);
    }

    public async Task<RoomTypeFormViewModel?> GetForEditAsync(int id)
    {
        var entity = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
        {
            return null;
        }

        return new RoomTypeFormViewModel
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            StandardCapacity = entity.StandardCapacity,
            MaxCapacity = entity.MaxCapacity,
            BasePricePerNight = entity.BasePricePerNight,
            ExtraGuestFeePerNight = entity.ExtraGuestFeePerNight,
            ExtraBedFeePerNight = entity.ExtraBedFeePerNight,
            SelectedAmenities = SplitAmenities(entity.Amenities),
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task<ServiceResult> CreateAsync(RoomTypeFormViewModel form)
    {
        var capacityCheck = ValidateCapacity(form);
        if (capacityCheck is not null)
        {
            return capacityCheck;
        }

        var code = form.Code.Trim().ToUpperInvariant();

        if (await _db.RoomTypes.AnyAsync(t => t.Code == code))
        {
            return ServiceResult.Fail($"Mã loại phòng \"{code}\" đã tồn tại.", nameof(form.Code));
        }

        var entity = new RoomType
        {
            Code = code,
            Name = form.Name.Trim(),
            StandardCapacity = form.StandardCapacity,
            MaxCapacity = form.MaxCapacity,
            BasePricePerNight = form.BasePricePerNight,
            ExtraGuestFeePerNight = form.ExtraGuestFeePerNight,
            ExtraBedFeePerNight = form.ExtraBedFeePerNight,
            Amenities = JoinAmenities(form.SelectedAmenities),
            Description = form.Description?.Trim(),
            IsActive = true
        };

        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync();

        // Ghi log sau khi lưu để có Id thật của bản ghi.
        _audit.Log("CreateRoomType", nameof(RoomType), entity.Id.ToString(),
            newValue: $"{entity.Code} — {entity.Name} — {entity.BasePricePerNight:N0} ₫/đêm");
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(RoomTypeFormViewModel form)
    {
        var capacityCheck = ValidateCapacity(form);
        if (capacityCheck is not null)
        {
            return capacityCheck;
        }

        var entity = await _db.RoomTypes.FirstOrDefaultAsync(t => t.Id == form.Id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy loại phòng cần sửa.");
        }

        // Mã loại phòng không sửa được sau khi tạo (SCR-A02) — bỏ qua giá trị gửi lên.
        var oldPrice = entity.BasePricePerNight;

        entity.Name = form.Name.Trim();
        entity.StandardCapacity = form.StandardCapacity;
        entity.MaxCapacity = form.MaxCapacity;
        entity.BasePricePerNight = form.BasePricePerNight;
        entity.ExtraGuestFeePerNight = form.ExtraGuestFeePerNight;
        entity.ExtraBedFeePerNight = form.ExtraBedFeePerNight;
        entity.Amenities = JoinAmenities(form.SelectedAmenities);
        entity.Description = form.Description?.Trim();
        entity.IsActive = form.IsActive;

        // BR-11: mọi lần sửa giá đều phải ghi lại giá cũ → giá mới.
        if (oldPrice != entity.BasePricePerNight)
        {
            _audit.Log("ChangeRoomTypePrice", nameof(RoomType), entity.Id.ToString(),
                oldValue: $"{oldPrice:N0} ₫/đêm",
                newValue: $"{entity.BasePricePerNight:N0} ₫/đêm");
        }

        // Giảm sức chứa tối đa xuống dưới số khách đang thực ở: cảnh báo nhưng vẫn lưu,
        // không đuổi khách; chỉ ảnh hưởng lần nhận phòng sau (SCR-A02).
        var overCapacityRooms = await _db.Stays
            .Where(s => s.Status == StayStatus.CheckedIn
                        && s.Room.RoomTypeId == entity.Id
                        && s.Guests.Count > form.MaxCapacity)
            .Select(s => s.Room.RoomNumber)
            .ToListAsync();

        await _db.SaveChangesAsync();

        var warning = overCapacityRooms.Count > 0
            ? $"Đã lưu, nhưng {overCapacityRooms.Count} phòng đang có nhiều khách hơn sức chứa tối đa mới "
              + $"({string.Join(", ", overCapacityRooms)}). Khách hiện tại không bị ảnh hưởng."
            : null;

        return ServiceResult.Ok(warning);
    }

    public async Task<ServiceResult> DeactivateAsync(int id)
    {
        var entity = await _db.RoomTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy loại phòng.");
        }

        if (!entity.IsActive)
        {
            return ServiceResult.Fail("Loại phòng này đã ngừng sử dụng.");
        }

        // SCR-A01: chỉ ngừng được khi không còn phòng đang hoạt động nào thuộc loại này.
        var activeRooms = await _db.Rooms.CountAsync(r => r.RoomTypeId == id && r.IsActive);
        if (activeRooms > 0)
        {
            return ServiceResult.Fail(
                $"Không thể ngừng sử dụng: còn {activeRooms} phòng đang hoạt động thuộc loại này.");
        }

        entity.IsActive = false;

        _audit.Log("DeactivateRoomType", nameof(RoomType), entity.Id.ToString(),
            oldValue: "Đang sử dụng", newValue: "Ngừng sử dụng");

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<RoomType>> GetActiveAsync()
        => await _db.RoomTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync();

    /// <summary>Ràng buộc liên trường, DataAnnotations không diễn đạt được.</summary>
    private static ServiceResult? ValidateCapacity(RoomTypeFormViewModel form)
        => form.MaxCapacity < form.StandardCapacity
            ? ServiceResult.Fail("Sức chứa tối đa phải lớn hơn hoặc bằng sức chứa chuẩn.",
                nameof(form.MaxCapacity))
            : null;

    private static string? JoinAmenities(IEnumerable<string>? amenities)
    {
        var list = amenities?.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray() ?? Array.Empty<string>();
        return list.Length == 0 ? null : string.Join(", ", list);
    }

    private static List<string> SplitAmenities(string? amenities)
        => string.IsNullOrWhiteSpace(amenities)
            ? new List<string>()
            : amenities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
