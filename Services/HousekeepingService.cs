using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Buồng phòng — nhóm E (SCR-E01, E03, E04), FR-E04, FR-E05, FR-E07, FR-E08.
///
/// Không có vai trò buồng phòng riêng: lễ tân bấm hộ. Trường người xác nhận trên
/// <see cref="HousekeepingTask"/> ghi người bấm, không phải người cầm chổi (đừng dùng đo năng suất).
/// Một phòng chỉ có một tác vụ dọn đang mở (đã chốt bằng index UX_Housekeeping_OpenPerRoom).
/// </summary>
public interface IHousekeepingService
{
    Task<HousekeepingBoardViewModel> BuildBoardAsync(HousekeepingBoardViewModel filter);
    Task<ServiceResult> StartCleaningAsync(int roomId, int employeeId);
    Task<ServiceResult> CompleteCleaningAsync(int roomId, int employeeId);
    Task<ServiceResult> MarkRepairedAsync(int roomId, int employeeId);

    Task<CreateRequestViewModel> BuildCreateRequestFormAsync(int? roomId);
    Task FillRequestFormOptionsAsync(CreateRequestViewModel form);
    Task<ServiceResult> CreateRequestAsync(CreateRequestViewModel form, int employeeId);

    Task<RequestListViewModel> BuildRequestsAsync(RequestListViewModel filter, int page);
    Task<ServiceResult> AssignRequestAsync(int id, int assignTo, int employeeId);
    Task<ServiceResult> CompleteRequestAsync(int id, int employeeId, string? resolution, bool roomFixed);
    Task<ServiceResult> CancelRequestAsync(int id, int employeeId);
}

/// <inheritdoc />
public class HousekeepingService : IHousekeepingService
{
    private static readonly RequestStatus[] OpenRequestStatuses = { RequestStatus.New, RequestStatus.InProgress };
    private static readonly HousekeepingTaskStatus[] OpenTaskStatuses =
        { HousekeepingTaskStatus.Pending, HousekeepingTaskStatus.InProgress };

    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly ITransactionRunner _tx;
    private readonly INumberSequenceService _numbers;

    public HousekeepingService(
        HotelDbContext db, IAuditService audit, ITransactionRunner tx, INumberSequenceService numbers)
    {
        _db = db;
        _audit = audit;
        _tx = tx;
        _numbers = numbers;
    }

    // ---------- SCR-E01 ----------

    public async Task<HousekeepingBoardViewModel> BuildBoardAsync(HousekeepingBoardViewModel filter)
    {
        var today = DateTime.Now.Date;

        var query = _db.Rooms.AsNoTracking().Where(r => r.IsActive);

        if (filter.Floor is not null)
        {
            query = query.Where(r => r.Floor == filter.Floor);
        }

        if (filter.Status is not null)
        {
            query = query.Where(r => r.Status == filter.Status);
        }
        else if (filter.OnlyActionable)
        {
            query = query.Where(r => r.Status == RoomStatus.Dirty || r.Status == RoomStatus.Maintenance);
        }

        var rooms = await query
            .Select(r => new
            {
                r.Id,
                r.RoomNumber,
                r.Floor,
                RoomTypeName = r.RoomType.Name,
                r.Status,
                r.StatusNote,
                r.UpdatedAt,
                r.CreatedAt
            })
            .ToListAsync();

        var roomIds = rooms.Select(r => r.Id).ToList();

        var openTasks = await _db.HousekeepingTasks.AsNoTracking()
            .Where(t => roomIds.Contains(t.RoomId) && OpenTaskStatuses.Contains(t.Status))
            .ToListAsync();

        var openRequestCounts = await _db.ServiceRequests.AsNoTracking()
            .Where(sr => roomIds.Contains(sr.RoomId) && OpenRequestStatuses.Contains(sr.Status))
            .GroupBy(sr => sr.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomId, x => x.Count);

        var arrivalRoomIds = await _db.ReservationRooms.AsNoTracking()
            .Where(rr => rr.RoomId != null
                && rr.Reservation.Status == ReservationStatus.Confirmed
                && rr.Reservation.CheckInDate == today)
            .Select(rr => rr.RoomId!.Value)
            .Distinct()
            .ToListAsync();

        var arrivalSet = arrivalRoomIds.ToHashSet();

        var boardRooms = rooms.Select(r =>
        {
            var task = openTasks.FirstOrDefault(t => t.RoomId == r.Id && t.Status == HousekeepingTaskStatus.InProgress);
            return new BoardRoom
            {
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomTypeName,
                Status = r.Status,
                StatusNote = r.StatusNote,
                IsBeingCleaned = task is not null,
                CleaningStartedAt = task?.StartedAt,
                DirtySince = r.Status == RoomStatus.Dirty ? (r.UpdatedAt ?? r.CreatedAt) : null,
                OpenRequestCount = openRequestCounts.TryGetValue(r.Id, out var c) ? c : 0,
                IsUrgentArrival = r.Status == RoomStatus.Dirty && arrivalSet.Contains(r.Id)
            };
        });

        var floors = boardRooms
            .GroupBy(r => r.Floor)
            .OrderBy(g => g.Key)
            .Select(g => new HousekeepingFloorGroup
            {
                Floor = g.Key,
                Rooms = g.OrderByDescending(r => r.IsUrgentArrival)
                    .ThenBy(r => StatusRank(r.Status))
                    .ThenBy(r => r.DirtySince ?? DateTime.MaxValue)
                    .ThenBy(r => r.RoomNumber)
                    .ToList()
            })
            .ToList();

        filter.Floors = floors;
        filter.FloorOptions = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Floor)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();

        return filter;
    }

    public async Task<ServiceResult> StartCleaningAsync(int roomId, int employeeId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            return ServiceResult.Fail("Không tìm thấy phòng.");
        }

        if (room.Status != RoomStatus.Dirty)
        {
            return ServiceResult.Fail($"Chỉ bắt đầu dọn được phòng đang Chờ dọn (phòng {room.RoomNumber} đang {room.Status.ToDisplayName()}).");
        }

        var hasOpen = await _db.HousekeepingTasks
            .AnyAsync(t => t.RoomId == roomId && OpenTaskStatuses.Contains(t.Status));
        if (hasOpen)
        {
            return ServiceResult.Fail($"Phòng {room.RoomNumber} đang có người dọn.");
        }

        try
        {
            await _tx.ExecuteAsync(async () =>
            {
                _db.HousekeepingTasks.Add(new HousekeepingTask
                {
                    RoomId = roomId,
                    Status = HousekeepingTaskStatus.InProgress,
                    AssignedTo = employeeId,
                    StartedAt = DateTime.Now
                });
                _audit.Log("StartCleaning", nameof(Room), roomId.ToString(), newValue: $"Bắt đầu dọn phòng {room.RoomNumber}");
                await _db.SaveChangesAsync();
            });
        }
        catch (DbUpdateException)
        {
            // Index UX_Housekeeping_OpenPerRoom chặn hai tác vụ dọn mở cùng lúc.
            return ServiceResult.Fail($"Phòng {room.RoomNumber} đang có người dọn.");
        }

        return ServiceResult.Ok(message: $"Đã bắt đầu dọn phòng {room.RoomNumber}.");
    }

    public async Task<ServiceResult> CompleteCleaningAsync(int roomId, int employeeId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            return ServiceResult.Fail("Không tìm thấy phòng.");
        }

        var task = await _db.HousekeepingTasks
            .FirstOrDefaultAsync(t => t.RoomId == roomId && t.Status == HousekeepingTaskStatus.InProgress);
        if (task is null)
        {
            return ServiceResult.Fail($"Phòng {room.RoomNumber} chưa có tác vụ dọn nào đang mở.");
        }

        await _tx.ExecuteAsync(async () =>
        {
            task.Status = HousekeepingTaskStatus.Done;
            task.CompletedAt = DateTime.Now;
            task.AssignedTo ??= employeeId;

            room.Status = RoomStatus.Available;
            room.StatusNote = null;

            _audit.Log("CompleteCleaning", nameof(Room), roomId.ToString(),
                oldValue: RoomStatus.Dirty.ToDisplayName(), newValue: RoomStatus.Available.ToDisplayName());
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã dọn xong phòng {room.RoomNumber}, phòng sẵn sàng bán.");
    }

    public async Task<ServiceResult> MarkRepairedAsync(int roomId, int employeeId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            return ServiceResult.Fail("Không tìm thấy phòng.");
        }

        if (room.Status != RoomStatus.Maintenance)
        {
            return ServiceResult.Fail($"Phòng {room.RoomNumber} không ở trạng thái Bảo trì.");
        }

        // Sửa xong thì phòng bẩn, phải dọn lại rồi mới bán (REQUIREMENTS 4.1).
        room.Status = RoomStatus.Dirty;
        room.StatusNote = null;
        _audit.Log("MarkRepaired", nameof(Room), roomId.ToString(),
            oldValue: RoomStatus.Maintenance.ToDisplayName(), newValue: RoomStatus.Dirty.ToDisplayName());
        await _db.SaveChangesAsync();

        return ServiceResult.Ok(message: $"Phòng {room.RoomNumber} đã sửa xong, chuyển sang Chờ dọn.");
    }

    // ---------- SCR-E03 ----------

    public async Task<CreateRequestViewModel> BuildCreateRequestFormAsync(int? roomId)
    {
        var form = new CreateRequestViewModel { RoomId = roomId ?? 0 };
        await FillRequestFormOptionsAsync(form);
        return form;
    }

    public async Task FillRequestFormOptionsAsync(CreateRequestViewModel form)
    {
        form.RoomOptions = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = $"{r.RoomNumber} — {r.RoomType.Name} ({r.Status.ToDisplayName()})"
            })
            .ToListAsync();

        form.EmployeeOptions = await EmployeeOptionsAsync();
    }

    public async Task<ServiceResult> CreateRequestAsync(CreateRequestViewModel form, int employeeId)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == form.RoomId);
        if (room is null)
        {
            return ServiceResult.Fail("Vui lòng chọn phòng.", nameof(form.RoomId));
        }

        if (string.IsNullOrWhiteSpace(form.Description) || form.Description.Trim().Length < 10)
        {
            return ServiceResult.Fail("Mô tả cần ít nhất 10 ký tự.", nameof(form.Description));
        }

        var makeMaintenance = form.Type == RequestType.Maintenance
            && form.MakeRoomMaintenance
            && room.Status != RoomStatus.Occupied
            && room.Status != RoomStatus.OutOfService;

        var occupiedWarning = form.Type == RequestType.Maintenance && room.Status == RoomStatus.Occupied;

        var code = await _tx.ExecuteAsync(async () =>
        {
            var value = await _numbers.NextValueAsync("REQ", string.Empty);
            var requestCode = $"REQ-{value:D6}";

            _db.ServiceRequests.Add(new ServiceRequest
            {
                Code = requestCode,
                Type = form.Type,
                RoomId = room.Id,
                Description = form.Description.Trim(),
                Priority = occupiedWarning ? RequestPriority.Urgent : form.Priority,
                Status = RequestStatus.New,
                AssignedTo = form.AssignedTo
            });

            if (makeMaintenance)
            {
                room.Status = RoomStatus.Maintenance;
                room.StatusNote = form.Description.Trim();
            }

            _audit.Log(form.Type == RequestType.Maintenance ? "ReportMaintenance" : "CreateServiceRequest",
                nameof(ServiceRequest), requestCode,
                newValue: $"Phòng {room.RoomNumber}: {form.Description.Trim()}");

            await _db.SaveChangesAsync();
            return requestCode;
        });

        if (occupiedWarning)
        {
            return ServiceResult.Ok(
                warning: $"Phòng {room.RoomNumber} đang có khách — không đổi trạng thái phòng. Cân nhắc đổi phòng cho khách (SCR-D06).",
                message: $"Đã tạo yêu cầu {code}.");
        }

        if (makeMaintenance)
        {
            return ServiceResult.Ok(
                warning: $"Phòng {room.RoomNumber} đã chuyển sang Bảo trì và ngừng bán.",
                message: $"Đã tạo yêu cầu {code}.");
        }

        return ServiceResult.Ok(message: $"Đã tạo yêu cầu {code}.");
    }

    // ---------- SCR-E04 ----------

    public async Task<RequestListViewModel> BuildRequestsAsync(RequestListViewModel filter, int page)
    {
        var query = _db.ServiceRequests.AsNoTracking();

        if (filter.Type is not null)
        {
            query = query.Where(sr => sr.Type == filter.Type);
        }

        if (filter.Priority is not null)
        {
            query = query.Where(sr => sr.Priority == filter.Priority);
        }

        if (filter.Status is not null)
        {
            query = query.Where(sr => sr.Status == filter.Status);
        }
        else if (!filter.CustomFilter)
        {
            // Mặc định chỉ hiện yêu cầu chưa hoàn thành.
            query = query.Where(sr => OpenRequestStatuses.Contains(sr.Status));
        }

        var projected = query
            // Khẩn cấp lên đầu, rồi mới nhất trước.
            .OrderByDescending(sr => sr.Priority == RequestPriority.Urgent)
            .ThenByDescending(sr => sr.CreatedAt).ThenByDescending(sr => sr.Id)
            .Select(sr => new RequestListItem
            {
                Id = sr.Id,
                Code = sr.Code,
                Type = sr.Type,
                RoomNumber = sr.Room.RoomNumber,
                Description = sr.Description,
                Priority = sr.Priority,
                Status = sr.Status,
                CreatedByName = _db.Employees.Where(e => e.Id == sr.CreatedBy).Select(e => e.FullName).FirstOrDefault() ?? "—",
                AssignedName = sr.AssignedEmployee != null ? sr.AssignedEmployee.FullName : null,
                CreatedAt = sr.CreatedAt,
                CompletedAt = sr.CompletedAt,
                RoomIsUnderMaintenance = sr.Type == RequestType.Maintenance && sr.Room.Status == RoomStatus.Maintenance
            });

        filter.Results = await PagedList<RequestListItem>.CreateAsync(projected, page);
        filter.EmployeeOptions = await EmployeeOptionsAsync();
        return filter;
    }

    public async Task<ServiceResult> AssignRequestAsync(int id, int assignTo, int employeeId)
    {
        var request = await _db.ServiceRequests.FirstOrDefaultAsync(sr => sr.Id == id);
        if (request is null || !OpenRequestStatuses.Contains(request.Status))
        {
            return ServiceResult.Fail("Chỉ phân công yêu cầu đang mở.");
        }

        if (assignTo <= 0)
        {
            return ServiceResult.Fail("Vui lòng chọn người xử lý.");
        }

        request.AssignedTo = assignTo;
        if (request.Status == RequestStatus.New)
        {
            request.Status = RequestStatus.InProgress;
        }

        _audit.Log("AssignServiceRequest", nameof(ServiceRequest), request.Code, newValue: $"Giao cho nhân viên #{assignTo}");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: $"Đã phân công yêu cầu {request.Code}.");
    }

    public async Task<ServiceResult> CompleteRequestAsync(int id, int employeeId, string? resolution, bool roomFixed)
    {
        var request = await _db.ServiceRequests.Include(sr => sr.Room).FirstOrDefaultAsync(sr => sr.Id == id);
        if (request is null || !OpenRequestStatuses.Contains(request.Status))
        {
            return ServiceResult.Fail("Chỉ hoàn thành yêu cầu đang mở.");
        }

        await _tx.ExecuteAsync(async () =>
        {
            request.Status = RequestStatus.Completed;
            request.CompletedAt = DateTime.Now;
            request.AssignedTo ??= employeeId;
            request.Resolution = string.IsNullOrWhiteSpace(resolution) ? null : resolution.Trim();

            if (roomFixed && request.Type == RequestType.Maintenance && request.Room.Status == RoomStatus.Maintenance)
            {
                request.Room.Status = RoomStatus.Dirty;
                request.Room.StatusNote = null;
            }

            _audit.Log("CompleteServiceRequest", nameof(ServiceRequest), request.Code,
                reason: request.Resolution, newValue: "Hoàn thành");
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã hoàn thành yêu cầu {request.Code}.");
    }

    public async Task<ServiceResult> CancelRequestAsync(int id, int employeeId)
    {
        var request = await _db.ServiceRequests.FirstOrDefaultAsync(sr => sr.Id == id);
        if (request is null || !OpenRequestStatuses.Contains(request.Status))
        {
            return ServiceResult.Fail("Chỉ hủy yêu cầu đang mở.");
        }

        request.Status = RequestStatus.Cancelled;
        _audit.Log("CancelServiceRequest", nameof(ServiceRequest), request.Code, newValue: "Hủy");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: $"Đã hủy yêu cầu {request.Code}.");
    }

    // ---------- Helpers ----------

    private async Task<IReadOnlyList<SelectListItem>> EmployeeOptionsAsync()
        => await _db.Employees.AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FullName)
            .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.FullName })
            .ToListAsync();

    private static int StatusRank(RoomStatus status) => status switch
    {
        RoomStatus.Dirty => 0,
        RoomStatus.Maintenance => 1,
        RoomStatus.Reserved => 2,
        RoomStatus.Available => 3,
        RoomStatus.Occupied => 4,
        RoomStatus.OutOfService => 5,
        _ => 9
    };
}
