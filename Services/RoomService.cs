using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>Nghiệp vụ danh mục phòng — SCR-A03, SCR-A04, SCR-A05, FR-A02, FR-A03.</summary>
public interface IRoomService
{
    Task FillIndexAsync(RoomIndexViewModel vm, int page);
    Task<RoomFormViewModel?> GetForEditAsync(int id);
    Task<ServiceResult> CreateAsync(RoomFormViewModel form);
    Task<ServiceResult> UpdateAsync(RoomFormViewModel form);

    Task<RoomStatusFormViewModel?> GetStatusFormAsync(int roomId);
    Task<ServiceResult> UpdateStatusAsync(RoomStatusFormViewModel form);

    /// <summary>Danh sách đơn đặt trong tương lai của một phòng, dạng chuỗi để hiển thị.</summary>
    Task<IReadOnlyList<string>> GetFutureReservationsAsync(int roomId);

    /// <summary>Loại phòng đang dùng, cho ô chọn trên form phòng.</summary>
    Task<IReadOnlyList<SelectListItem>> GetRoomTypeOptionsAsync();
}

/// <inheritdoc />
public class RoomService : IRoomService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;

    public RoomService(HotelDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ---------- SCR-A03: danh sách ----------

    public async Task FillIndexAsync(RoomIndexViewModel vm, int page)
    {
        var query = _db.Rooms.AsNoTracking().Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
        {
            var k = vm.Keyword.Trim();
            query = query.Where(r => r.RoomNumber.Contains(k));
        }

        if (vm.Floor is not null)
        {
            query = query.Where(r => r.Floor == vm.Floor);
        }

        if (vm.RoomTypeId is not null)
        {
            query = query.Where(r => r.RoomTypeId == vm.RoomTypeId);
        }

        if (vm.Status is not null)
        {
            query = query.Where(r => r.Status == vm.Status);
        }

        var projected = query
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new RoomListItemViewModel
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType.Name,
                PricePerNight = r.RoomType.BasePricePerNight,
                Status = r.Status,
                StatusNote = r.StatusNote,
                IsActive = r.IsActive,
                CurrentGuestName = r.Stays
                    .Where(s => s.Status == StayStatus.CheckedIn)
                    .Select(s => s.PrimaryGuest.FullName)
                    .FirstOrDefault(),
                CurrentStayId = r.Stays
                    .Where(s => s.Status == StayStatus.CheckedIn)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefault()
            });

        // Dạng lưới cần nhìn toàn bộ sơ đồ nên không phân trang; dạng bảng thì phân trang.
        if (vm.IsGrid)
        {
            vm.AllForGrid = await projected.ToListAsync();
            vm.Results = new PagedList<RoomListItemViewModel>
            {
                Items = vm.AllForGrid,
                Page = 1,
                TotalItems = vm.AllForGrid.Count
            };
        }
        else
        {
            vm.Results = await PagedList<RoomListItemViewModel>.CreateAsync(projected, page);
        }

        vm.FloorOptions = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Floor)
            .Distinct()
            .OrderBy(f => f)
            .Select(f => new SelectListItem
            {
                Value = f.ToString(),
                Text = $"Tầng {f}"
            })
            .ToListAsync();

        vm.RoomTypeOptions = await BuildRoomTypeOptionsAsync();
    }

    // ---------- SCR-A04: thêm / sửa ----------

    public async Task<RoomFormViewModel?> GetForEditAsync(int id)
    {
        var entity = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null)
        {
            return null;
        }

        return new RoomFormViewModel
        {
            Id = entity.Id,
            RoomNumber = entity.RoomNumber,
            Floor = entity.Floor,
            RoomTypeId = entity.RoomTypeId,
            Status = entity.Status,
            Notes = entity.Notes,
            RoomTypeOptions = await BuildRoomTypeOptionsAsync()
        };
    }

    public async Task<ServiceResult> CreateAsync(RoomFormViewModel form)
    {
        var number = form.RoomNumber.Trim();

        // FR-A02: số phòng duy nhất toàn hệ thống, tính cả phòng đã ngừng dùng.
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == number))
        {
            return ServiceResult.Fail($"Số phòng \"{number}\" đã tồn tại.", nameof(form.RoomNumber));
        }

        if (!await _db.RoomTypes.AnyAsync(t => t.Id == form.RoomTypeId && t.IsActive))
        {
            return ServiceResult.Fail("Loại phòng không hợp lệ hoặc đã ngừng sử dụng.", nameof(form.RoomTypeId));
        }

        var entity = new Room
        {
            RoomNumber = number,
            Floor = form.Floor,
            RoomTypeId = form.RoomTypeId,
            Status = form.Status,
            Notes = form.Notes?.Trim(),
            IsActive = true
        };

        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync();

        _audit.Log("CreateRoom", nameof(Room), entity.Id.ToString(),
            newValue: $"Phòng {entity.RoomNumber}, tầng {entity.Floor}");
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(RoomFormViewModel form)
    {
        var entity = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == form.Id);
        if (entity is null)
        {
            return ServiceResult.Fail("Không tìm thấy phòng cần sửa.");
        }

        var number = form.RoomNumber.Trim();

        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == number && r.Id != entity.Id))
        {
            return ServiceResult.Fail($"Số phòng \"{number}\" đã thuộc về phòng khác.", nameof(form.RoomNumber));
        }

        var roomTypeChanged = entity.RoomTypeId != form.RoomTypeId;

        if (roomTypeChanged)
        {
            if (!await _db.RoomTypes.AnyAsync(t => t.Id == form.RoomTypeId && t.IsActive))
            {
                return ServiceResult.Fail("Loại phòng không hợp lệ hoặc đã ngừng sử dụng.", nameof(form.RoomTypeId));
            }

            // SCR-A04: không đổi loại phòng khi phòng đang có khách.
            if (await _db.Stays.AnyAsync(s => s.RoomId == entity.Id && s.Status == StayStatus.CheckedIn))
            {
                return ServiceResult.Fail(
                    "Không thể đổi loại phòng khi đang có khách lưu trú.", nameof(form.RoomTypeId));
            }

            // Có đơn đặt trong tương lai thì cảnh báo và yêu cầu Admin xác nhận.
            var affected = await GetFutureReservationsAsync(entity.Id);
            if (affected.Count > 0 && !form.ConfirmRoomTypeChange)
            {
                form.AffectedReservations = affected;
                return ServiceResult.Fail(
                    $"Phòng này còn {affected.Count} đơn đặt trong tương lai. "
                    + "Đổi loại phòng có thể khiến các đơn đó sai loại — tích xác nhận bên dưới nếu vẫn muốn đổi.",
                    nameof(form.ConfirmRoomTypeChange));
            }
        }

        var oldNumber = entity.RoomNumber;
        var oldRoomTypeId = entity.RoomTypeId;

        entity.RoomNumber = number;
        entity.Floor = form.Floor;
        entity.RoomTypeId = form.RoomTypeId;
        entity.Notes = form.Notes?.Trim();
        // Trạng thái không sửa ở đây — phải đi qua SCR-A05 để có lý do và audit log.

        if (roomTypeChanged)
        {
            var oldName = await _db.RoomTypes.Where(t => t.Id == oldRoomTypeId)
                .Select(t => t.Name).FirstOrDefaultAsync();
            var newName = await _db.RoomTypes.Where(t => t.Id == form.RoomTypeId)
                .Select(t => t.Name).FirstOrDefaultAsync();

            _audit.Log("ChangeRoomType", nameof(Room), entity.Id.ToString(),
                oldValue: oldName, newValue: newName,
                reason: $"Phòng {oldNumber}");
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    // ---------- SCR-A05: đổi trạng thái ----------

    /// <summary>
    /// Các chuyển trạng thái hợp lệ (docs/screens/02-catalog.md, SCR-A05).
    /// Cả Admin và Lễ tân đều được thực hiện mọi chuyển đổi ở đây; luật duy nhất còn lại
    /// là phòng Occupied không ai sửa tay được.
    /// </summary>
    private static readonly IReadOnlyDictionary<RoomStatus, RoomStatus[]> AllowedTransitions =
        new Dictionary<RoomStatus, RoomStatus[]>
        {
            [RoomStatus.Available] = new[] { RoomStatus.Dirty, RoomStatus.Maintenance, RoomStatus.OutOfService },
            [RoomStatus.Reserved] = new[] { RoomStatus.Dirty, RoomStatus.Maintenance, RoomStatus.OutOfService },
            [RoomStatus.Dirty] = new[] { RoomStatus.Available, RoomStatus.Maintenance, RoomStatus.OutOfService },
            [RoomStatus.Maintenance] = new[] { RoomStatus.Dirty, RoomStatus.OutOfService },
            [RoomStatus.OutOfService] = new[] { RoomStatus.Dirty },
            // Occupied: không có chuyển đổi thủ công nào.
            [RoomStatus.Occupied] = Array.Empty<RoomStatus>()
        };

    /// <summary>Các trạng thái bắt buộc nhập lý do.</summary>
    private static bool RequiresReason(RoomStatus status)
        => status is RoomStatus.Maintenance or RoomStatus.OutOfService;

    public async Task<RoomStatusFormViewModel?> GetStatusFormAsync(int roomId)
    {
        var room = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new { r.Id, r.RoomNumber, r.Status })
            .FirstOrDefaultAsync();

        if (room is null)
        {
            return null;
        }

        var allowed = AllowedTransitions.TryGetValue(room.Status, out var list) ? list : Array.Empty<RoomStatus>();

        return new RoomStatusFormViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            CurrentStatus = room.Status,
            NewStatus = allowed.FirstOrDefault(),
            AllowedStatuses = allowed
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToDisplayName()
                })
                .ToList(),
            AffectedReservations = await GetFutureReservationsAsync(room.Id)
        };
    }

    public async Task<ServiceResult> UpdateStatusAsync(RoomStatusFormViewModel form)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == form.RoomId);
        if (room is null)
        {
            return ServiceResult.Fail("Không tìm thấy phòng.");
        }

        if (room.Status == form.NewStatus)
        {
            return ServiceResult.Fail("Phòng đang ở đúng trạng thái này rồi.");
        }

        // Kiểm tra lại ở server: giao diện chỉ liệt kê trạng thái hợp lệ, nhưng không được tin nó.
        var allowed = AllowedTransitions.TryGetValue(room.Status, out var list) ? list : Array.Empty<RoomStatus>();
        if (!allowed.Contains(form.NewStatus))
        {
            return room.Status == RoomStatus.Occupied
                ? ServiceResult.Fail(
                    "Phòng đang có khách. Trạng thái chỉ thay đổi qua check-out hoặc đổi phòng.")
                : ServiceResult.Fail(
                    $"Không thể chuyển từ \"{room.Status.ToDisplayName()}\" sang \"{form.NewStatus.ToDisplayName()}\".");
        }

        if (RequiresReason(form.NewStatus) && string.IsNullOrWhiteSpace(form.Reason))
        {
            return ServiceResult.Fail(
                $"Vui lòng nhập lý do khi chuyển sang \"{form.NewStatus.ToDisplayName()}\".", nameof(form.Reason));
        }

        var oldStatus = room.Status;
        room.Status = form.NewStatus;
        room.StatusNote = RequiresReason(form.NewStatus) ? form.Reason?.Trim() : null;

        // BR-11: mọi lần đổi trạng thái đều ghi log ai, từ đâu sang đâu, vì sao.
        _audit.Log("ChangeRoomStatus", nameof(Room), room.Id.ToString(),
            oldValue: oldStatus.ToDisplayName(),
            newValue: form.NewStatus.ToDisplayName(),
            reason: form.Reason?.Trim());

        await _db.SaveChangesAsync();

        // Ngừng bán một phòng còn đơn đặt thì phải nhắc người dùng đi xếp lại phòng.
        string? warning = null;
        if (RequiresReason(form.NewStatus))
        {
            var affected = await GetFutureReservationsAsync(room.Id);
            if (affected.Count > 0)
            {
                warning = $"Phòng {room.RoomNumber} còn {affected.Count} đơn đặt trong tương lai "
                          + $"({string.Join("; ", affected)}). Cần xếp lại phòng cho các đơn này.";
            }
        }

        return ServiceResult.Ok(warning);
    }

    // ---------- dùng chung ----------

    public async Task<IReadOnlyList<string>> GetFutureReservationsAsync(int roomId)
    {
        var today = DateTime.Today;

        return await _db.ReservationRooms.AsNoTracking()
            .Where(rr => rr.RoomId == roomId
                         && rr.Reservation.CheckOutDate >= today
                         && (rr.Reservation.Status == ReservationStatus.Confirmed
                             || rr.Reservation.Status == ReservationStatus.Draft))
            .OrderBy(rr => rr.Reservation.CheckInDate)
            .Select(rr => rr.Reservation.Code
                          + " (" + rr.Reservation.CheckInDate.ToString("dd/MM")
                          + "–" + rr.Reservation.CheckOutDate.ToString("dd/MM") + ")")
            .Take(10)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SelectListItem>> GetRoomTypeOptionsAsync()
        => await _db.RoomTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Code + " — " + t.Name
            })
            .ToListAsync();

    private Task<IReadOnlyList<SelectListItem>> BuildRoomTypeOptionsAsync() => GetRoomTypeOptionsAsync();
}
