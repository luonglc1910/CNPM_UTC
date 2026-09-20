using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Nghiệp vụ đặt phòng — nhóm C (SCR-C01…C09), FR-C01…C09, BR-02, BR-05, BR-06.
///
/// Dùng lại hạ tầng SP0: <see cref="IAvailabilityService"/> (chống trùng), <see cref="IPricingService"/>
/// (số đêm, phụ thu, phí hủy), <see cref="INumberSequenceService"/> (mã đơn), <see cref="ITransactionRunner"/>
/// (thao tác nhiều bước), <see cref="IShiftService"/> (thu/hoàn tiền phải có ca mở — BR-10).
/// </summary>
public interface IReservationService
{
    Task<ReservationIndexViewModel> BuildIndexAsync(ReservationIndexViewModel filter, int page);
    Task BuildAvailabilityAsync(AvailabilitySearchViewModel vm);

    Task<ReservationFormViewModel> BuildCreateFormAsync(DateTime? checkIn, DateTime? checkOut, int? roomTypeId, int? roomId);
    Task<(ServiceResult Result, int Id)> CreateAsync(ReservationFormViewModel form, bool isAdmin);

    Task<ReservationDetailsViewModel?> GetDetailsAsync(int id);

    Task<ReservationFormViewModel?> BuildEditFormAsync(int id);
    Task<(ServiceResult Result, int Id)> UpdateAsync(ReservationFormViewModel form, bool isAdmin);

    Task<DepositFormViewModel?> BuildDepositFormAsync(int id, int employeeId);
    Task<ServiceResult> TakeDepositAsync(DepositFormViewModel form, int employeeId);

    Task<CancelReservationViewModel?> BuildCancelAsync(int id, int employeeId, bool isAdmin);
    Task<ServiceResult> CancelAsync(CancelReservationViewModel form, int employeeId, bool isAdmin);

    Task<NoShowListViewModel> BuildNoShowListAsync();
    Task<ServiceResult> MarkNoShowAsync(int id, int employeeId);
    Task<ServiceResult> ExtendHoldAsync(int id, int hours, int employeeId);

    Task FillFormOptionsAsync(ReservationFormViewModel form);

    /// <summary>Sơ đồ phòng theo ngày — SCR-C03.</summary>
    Task<RoomChartViewModel> BuildRoomChartAsync(DateTime? from, int days);
    /// <summary>Trạng thái đơn đặt phòng, null nếu không có. Chỉ dùng để giải thích lỗi.</summary>
    Task<(ReservationStatus? Status, string? Code)> GetStatusAsync(int reservationId);

}

/// <inheritdoc />
public class ReservationService : IReservationService
{
    private static readonly ReservationStatus[] EditableStatuses =
        { ReservationStatus.Draft, ReservationStatus.Confirmed };

    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly IAvailabilityService _availability;
    private readonly IPricingService _pricing;
    private readonly INumberSequenceService _numbers;
    private readonly ITransactionRunner _tx;
    private readonly IShiftService _shifts;
    private readonly ISettingsReader _settings;

    public ReservationService(
        HotelDbContext db, IAuditService audit, IAvailabilityService availability, IPricingService pricing,
        INumberSequenceService numbers, ITransactionRunner tx, IShiftService shifts, ISettingsReader settings)
    {
        _db = db;
        _audit = audit;
        _availability = availability;
        _pricing = pricing;
        _numbers = numbers;
        _tx = tx;
        _shifts = shifts;
        _settings = settings;
    }

    // ---------- SCR-C01 ----------

    public async Task<ReservationIndexViewModel> BuildIndexAsync(ReservationIndexViewModel filter, int page)
    {
        var query = _db.Reservations.AsNoTracking();

        if (!filter.CustomFilter && !filter.HasFilter)
        {
            // Mặc định: việc lễ tân cần xử lý — đơn Confirmed có ngày đến từ hôm nay trở đi (SCR-C01).
            var today = DateTime.Now.Date;
            query = query.Where(r => r.Status == ReservationStatus.Confirmed && r.CheckInDate >= today);
        }
        else
        {
            if (filter.CheckInFrom is not null)
            {
                query = query.Where(r => r.CheckInDate >= filter.CheckInFrom.Value.Date);
            }

            if (filter.CheckInTo is not null)
            {
                query = query.Where(r => r.CheckInDate <= filter.CheckInTo.Value.Date);
            }

            if (filter.Status is not null)
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            if (filter.Source is not null)
            {
                query = query.Where(r => r.Source == filter.Source);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var k = filter.Keyword.Trim();
                query = query.Where(r => r.Code.Contains(k)
                    || r.PrimaryGuest.FullName.Contains(k)
                    || r.PrimaryGuest.PhoneNumber.Contains(k));
            }
        }

        var projected = query
            .OrderByDescending(r => r.CheckInDate).ThenByDescending(r => r.Id)
            .Select(r => new ReservationListItem
            {
                Id = r.Id,
                Code = r.Code,
                GuestName = r.PrimaryGuest.FullName,
                PhoneNumber = r.PrimaryGuest.PhoneNumber,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Nights = r.Nights,
                RoomCount = r.Rooms.Count,
                RoomTypeSummary = string.Join(", ", r.Rooms.Select(rr => rr.RoomType.Code)),
                EstimatedTotal = r.EstimatedTotal,
                DepositPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => (decimal?)d.Amount) ?? 0m,
                Status = r.Status,
                Source = r.Source
            });

        filter.Results = await PagedList<ReservationListItem>.CreateAsync(projected, page);
        return filter;
    }

    // ---------- SCR-C02 ----------

    public async Task BuildAvailabilityAsync(AvailabilitySearchViewModel vm)
    {
        vm.RoomTypeOptions = await RoomTypeOptionsAsync();

        if (vm.CheckIn is null || vm.CheckOut is null)
        {
            return;
        }

        vm.Searched = true;

        if (vm.CheckOut.Value.Date <= vm.CheckIn.Value.Date)
        {
            vm.Nights = 0;
            return;
        }

        var checkIn = vm.CheckIn.Value.Date;
        var checkOut = vm.CheckOut.Value.Date;
        vm.Nights = _pricing.CountNights(checkIn, checkOut);

        var available = await _availability.GetAvailableRoomsAsync(checkIn, checkOut, vm.RoomTypeId, vm.Guests);

        vm.Groups = available
            .GroupBy(r => new { r.RoomTypeId, r.RoomTypeName, r.RoomTypeCode, r.PricePerNight, r.StandardCapacity, r.MaxCapacity })
            .Select(g => new AvailabilityGroup
            {
                RoomTypeId = g.Key.RoomTypeId,
                RoomTypeName = g.Key.RoomTypeName,
                RoomTypeCode = g.Key.RoomTypeCode,
                PricePerNight = g.Key.PricePerNight,
                StandardCapacity = g.Key.StandardCapacity,
                MaxCapacity = g.Key.MaxCapacity,
                AvailableRooms = g.Select(r => new AvailabilityRoomOption
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Floor = r.Floor,
                    NeedsCleaning = r.NeedsCleaning
                }).ToList()
            })
            .OrderBy(g => g.RoomTypeCode)
            .ToList();
    }

    // ---------- SCR-C04 ----------

    public async Task<ReservationFormViewModel> BuildCreateFormAsync(
        DateTime? checkIn, DateTime? checkOut, int? roomTypeId, int? roomId)
    {
        var form = new ReservationFormViewModel
        {
            CheckInDate = (checkIn ?? DateTime.Now).Date,
            CheckOutDate = (checkOut ?? DateTime.Now.AddDays(1)).Date
        };

        if (roomTypeId is not null)
        {
            var price = await _db.RoomTypes.Where(t => t.Id == roomTypeId)
                .Select(t => (decimal?)t.BasePricePerNight).FirstOrDefaultAsync() ?? 0m;

            form.Rooms.Add(new ReservationRoomInput
            {
                RoomTypeId = roomTypeId.Value,
                RoomId = roomId,
                Adults = 1,
                PricePerNight = price
            });
        }

        if (form.Rooms.Count == 0)
        {
            form.Rooms.Add(new ReservationRoomInput { Adults = 1 });
        }

        await FillFormOptionsAsync(form);
        return form;
    }

    public async Task<(ServiceResult Result, int Id)> CreateAsync(ReservationFormViewModel form, bool isAdmin)
    {
        var prepared = await PrepareRoomsAsync(form, isAdmin, excludeReservationId: null);
        if (!prepared.Result.Succeeded)
        {
            return (prepared.Result, 0);
        }

        var nights = _pricing.CountNights(form.CheckInDate, form.CheckOutDate);
        var confirmNow = form.ConfirmNow;

        var reservation = new Reservation
        {
            PrimaryGuestId = form.PrimaryGuestId,
            CheckInDate = form.CheckInDate.Date,
            CheckOutDate = form.CheckOutDate.Date,
            Nights = nights,
            Status = confirmNow ? ReservationStatus.Confirmed : ReservationStatus.Draft,
            Source = form.Source,
            SpecialRequests = Trimmed(form.SpecialRequests),
            InternalNotes = Trimmed(form.InternalNotes),
            EstimatedTotal = prepared.EstimatedTotal,
            Rooms = prepared.Rooms
        };

        if (confirmNow)
        {
            reservation.HoldUntil = await ComputeHoldUntilAsync(form.CheckInDate);
        }

        var primaryGuestBlacklisted = await _db.Guests.AsNoTracking()
            .Where(g => g.Id == form.PrimaryGuestId)
            .Select(g => g.IsBlacklisted)
            .FirstOrDefaultAsync();

        var newId = await _tx.ExecuteAsync(async () =>
        {
            reservation.Code = await _numbers.NextReservationCodeAsync(DateTime.Now);
            _db.Reservations.Add(reservation);
            _audit.Log("CreateReservation", nameof(Reservation), null,
                newValue: $"{reservation.Code} ({reservation.CheckInDate:dd/MM/yyyy}–{reservation.CheckOutDate:dd/MM/yyyy}, {reservation.Rooms.Count} phòng)");

            // Cảnh báo khách hạn chế không tự chặn (SCR-B05); vẫn tạo đơn thì phải để lại dấu vết
            // để SCR-G04 đếm được số lần bỏ qua.
            if (primaryGuestBlacklisted)
            {
                _audit.Log(AuditActions.OverrideBlacklistWarning, nameof(Reservation), null,
                    reason: "Tạo đơn cho khách nằm trong danh sách hạn chế");
            }
            await _db.SaveChangesAsync();
            return reservation.Id;
        });

        return (ServiceResult.Ok(message: $"Đã tạo đơn {reservation.Code}."), newId);
    }

    // ---------- SCR-C05 ----------

    public async Task<ReservationDetailsViewModel?> GetDetailsAsync(int id)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Rooms).ThenInclude(rr => rr.RoomType)
            .Include(x => x.Rooms).ThenInclude(rr => rr.Room)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null)
        {
            return null;
        }

        var depositPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => d.Amount);
        var createdBy = r.CreatedBy is null ? string.Empty
            : await _db.Employees.Where(e => e.Id == r.CreatedBy).Select(e => e.FullName).FirstOrDefaultAsync() ?? string.Empty;

        var today = DateTime.Now.Date;

        return new ReservationDetailsViewModel
        {
            Reservation = r,
            GuestName = r.PrimaryGuest.FullName,
            PhoneNumber = r.PrimaryGuest.PhoneNumber,
            IdNumber = r.PrimaryGuest.IdNumber,
            GuestBlacklisted = r.PrimaryGuest.IsBlacklisted,
            CreatedByName = createdBy,
            Rooms = r.Rooms.Select(rr => new ReservationRoomLine
            {
                RoomTypeName = rr.RoomType.Name,
                RoomNumber = rr.Room != null ? rr.Room.RoomNumber : null,
                Adults = rr.Adults,
                Children = rr.Children,
                PricePerNight = rr.PricePerNight,
                LineTotal = rr.PricePerNight * r.Nights
            }).ToList(),
            Deposits = r.Deposits.OrderBy(d => d.ReceivedAt).Select(d => new ReservationDepositLine
            {
                ReceivedAt = d.ReceivedAt,
                Amount = d.Amount,
                Status = d.Status
            }).ToList(),
            DepositPaid = depositPaid,
            EstimatedRemaining = r.EstimatedTotal - depositPaid,
            CanEdit = EditableStatuses.Contains(r.Status),
            CanTakeDeposit = EditableStatuses.Contains(r.Status),
            CanCheckIn = r.Status == ReservationStatus.Confirmed && r.CheckInDate <= today,
            CanCancel = EditableStatuses.Contains(r.Status),
            CanMarkNoShow = r.Status == ReservationStatus.Confirmed && r.CheckInDate <= today
        };
    }

    // ---------- SCR-C06 ----------

    public async Task<ReservationFormViewModel?> BuildEditFormAsync(int id)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null || !EditableStatuses.Contains(r.Status))
        {
            return null;
        }

        var form = new ReservationFormViewModel
        {
            Id = r.Id,
            PrimaryGuestId = r.PrimaryGuestId,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Source = r.Source,
            SpecialRequests = r.SpecialRequests,
            InternalNotes = r.InternalNotes,
            ConfirmNow = r.Status == ReservationStatus.Confirmed,
            Rooms = r.Rooms.Select(rr => new ReservationRoomInput
            {
                RoomTypeId = rr.RoomTypeId,
                RoomId = rr.RoomId,
                Adults = rr.Adults,
                Children = rr.Children,
                PricePerNight = rr.PricePerNight
            }).ToList()
        };

        await FillFormOptionsAsync(form);
        return form;
    }

    public async Task<(ServiceResult Result, int Id)> UpdateAsync(ReservationFormViewModel form, bool isAdmin)
    {
        var r = await _db.Reservations
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == form.Id);

        if (r is null)
        {
            return (ServiceResult.Fail("Không tìm thấy đơn đặt phòng."), 0);
        }

        if (!EditableStatuses.Contains(r.Status))
        {
            return (ServiceResult.Fail("Chỉ sửa được đơn ở trạng thái Nháp hoặc Đã xác nhận."), 0);
        }

        var prepared = await PrepareRoomsAsync(form, isAdmin, excludeReservationId: r.Id);
        if (!prepared.Result.Succeeded)
        {
            return (prepared.Result, 0);
        }

        r.PrimaryGuestId = form.PrimaryGuestId;
        r.CheckInDate = form.CheckInDate.Date;
        r.CheckOutDate = form.CheckOutDate.Date;
        r.Nights = _pricing.CountNights(form.CheckInDate, form.CheckOutDate);
        r.Source = form.Source;
        r.SpecialRequests = Trimmed(form.SpecialRequests);
        r.InternalNotes = Trimmed(form.InternalNotes);
        r.EstimatedTotal = prepared.EstimatedTotal;

        _db.ReservationRooms.RemoveRange(r.Rooms);
        r.Rooms = prepared.Rooms;

        _audit.Log("UpdateReservation", nameof(Reservation), r.Id.ToString(),
            newValue: $"{r.CheckInDate:dd/MM/yyyy}–{r.CheckOutDate:dd/MM/yyyy}, {prepared.Rooms.Count} phòng, dự kiến {r.EstimatedTotal:N0} ₫");

        await _db.SaveChangesAsync();
        return (ServiceResult.Ok(message: $"Đã cập nhật đơn {r.Code}."), r.Id);
    }

    // ---------- SCR-C07 ----------

    public async Task<DepositFormViewModel?> BuildDepositFormAsync(int id, int employeeId)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null || !EditableStatuses.Contains(r.Status))
        {
            return null;
        }

        var alreadyPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => d.Amount);
        var suggested = await SuggestedDepositAsync(r);

        return new DepositFormViewModel
        {
            ReservationId = r.Id,
            ReservationCode = r.Code,
            GuestName = r.PrimaryGuest.FullName,
            EstimatedTotal = r.EstimatedTotal,
            AlreadyPaid = alreadyPaid,
            SuggestedAmount = Math.Max(0, suggested - alreadyPaid),
            Amount = Math.Max(0, suggested - alreadyPaid),
            HasOpenShift = await _shifts.GetOpenShiftAsync(employeeId) is not null
        };
    }

    public async Task<ServiceResult> TakeDepositAsync(DepositFormViewModel form, int employeeId)
    {
        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        if (shift is null)
        {
            return ServiceResult.Fail("Bạn cần mở ca làm việc trước khi thu tiền (BR-10).");
        }

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == form.ReservationId);
        if (r is null || !EditableStatuses.Contains(r.Status))
        {
            return ServiceResult.Fail("Chỉ thu cọc cho đơn Nháp hoặc Đã xác nhận.");
        }

        if (form.Amount <= 0)
        {
            return ServiceResult.Fail("Số tiền cọc phải lớn hơn 0.", nameof(form.Amount));
        }

        if (form.Method != PaymentMethod.Cash && string.IsNullOrWhiteSpace(form.TransactionRef))
        {
            return ServiceResult.Fail("Chuyển khoản/Thẻ bắt buộc nhập mã giao dịch.", nameof(form.TransactionRef));
        }

        await _tx.ExecuteAsync(async () =>
        {
            var deposit = new Deposit
            {
                ReservationId = r.Id,
                Amount = form.Amount,
                Status = DepositStatus.Held,
                ReceivedAt = DateTime.Now
            };
            _db.Deposits.Add(deposit);
            await _db.SaveChangesAsync();

            _db.Payments.Add(new Payment
            {
                Type = PaymentType.Deposit,
                Method = form.Method,
                Amount = form.Amount,
                TransactionRef = Trimmed(form.TransactionRef),
                PaidAt = DateTime.Now,
                CashierShiftId = shift.Id,
                DepositId = deposit.Id,
                ReservationId = r.Id,
                Notes = Trimmed(form.Notes)
            });

            // Đã cọc thì đơn thoát diện hết hạn giữ chỗ (BR-05).
            r.HoldUntil = null;

            _audit.Log("TakeDeposit", nameof(Reservation), r.Id.ToString(),
                newValue: $"Cọc {form.Amount:N0} ₫ ({form.Method.ToDisplayName()})");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã thu cọc {form.Amount:N0} ₫ cho đơn {r.Code}.");
    }

    // ---------- SCR-C08 ----------

    public async Task<CancelReservationViewModel?> BuildCancelAsync(int id, int employeeId, bool isAdmin)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null || !EditableStatuses.Contains(r.Status))
        {
            return null;
        }

        var vm = new CancelReservationViewModel
        {
            ReservationId = r.Id,
            ReservationCode = r.Code,
            GuestName = r.PrimaryGuest.FullName,
            CheckInDate = r.CheckInDate,
            HasOpenShift = await _shifts.GetOpenShiftAsync(employeeId) is not null,
            IsAdmin = isAdmin
        };

        await FillCancelNumbersAsync(vm, r, waiveFee: false);
        return vm;
    }

    public async Task<ServiceResult> CancelAsync(CancelReservationViewModel form, int employeeId, bool isAdmin)
    {
        var r = await _db.Reservations
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == form.ReservationId);

        if (r is null || !EditableStatuses.Contains(r.Status))
        {
            return ServiceResult.Fail("Chỉ hủy được đơn Nháp hoặc Đã xác nhận.");
        }

        if (string.IsNullOrWhiteSpace(form.Reason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do hủy.", nameof(form.Reason));
        }

        var waive = form.WaiveFee && isAdmin;
        var numbers = await ComputeCancelNumbersAsync(r, waive);
        var depositPaid = numbers.DepositPaid;
        var refund = numbers.Refund;

        // Có tiền cọc thì việc thu phí hủy / hoàn tiền là giao dịch tiền — cần ca mở (BR-10).
        if (depositPaid > 0 && await _shifts.GetOpenShiftAsync(employeeId) is null)
        {
            return ServiceResult.Fail("Đơn đã có cọc — cần mở ca làm việc để xử lý phí hủy/hoàn tiền.");
        }

        var shift = await _shifts.GetOpenShiftAsync(employeeId);

        await _tx.ExecuteAsync(async () =>
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelledAt = DateTime.Now;
            r.CancelledBy = employeeId;
            r.CancellationReason = form.Reason.Trim();
            r.CancellationFee = numbers.Fee;

            foreach (var d in r.Deposits.Where(d => d.Status == DepositStatus.Held))
            {
                d.Status = numbers.Fee > 0 ? DepositStatus.Forfeited : DepositStatus.Refunded;
                d.RefundedAmount = refund > 0 && depositPaid > 0 ? refund * (d.Amount / depositPaid) : 0m;
            }

            if (refund > 0 && shift is not null)
            {
                _db.Payments.Add(new Payment
                {
                    Type = PaymentType.Refund,
                    Method = PaymentMethod.Cash,
                    Amount = -refund,
                    PaidAt = DateTime.Now,
                    CashierShiftId = shift.Id,
                    ReservationId = r.Id,
                    Notes = "Hoàn cọc khi hủy đơn"
                });
            }

            _audit.Log(waive ? "CancelReservationWaiveFee" : "CancelReservation",
                nameof(Reservation), r.Id.ToString(),
                reason: form.Reason.Trim(),
                newValue: $"Phí hủy {numbers.Fee:N0} ₫, hoàn {refund:N0} ₫");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã hủy đơn {r.Code}. Phí hủy {numbers.Fee:N0} ₫, hoàn khách {refund:N0} ₫.");
    }

    // ---------- SCR-C09 ----------

    public async Task<NoShowListViewModel> BuildNoShowListAsync()
    {
        var now = DateTime.Now;
        var today = now.Date;

        var candidates = await _db.Reservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Confirmed && r.CheckInDate <= today)
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();

        var holdHour = await _settings.GetIntAsync(SystemSettingKeys.HoldUntilHour);

        var items = candidates
            .Select(r =>
            {
                var holdUntil = r.HoldUntil ?? r.CheckInDate.Date.AddHours(holdHour);
                return new OverdueReservationItem
                {
                    Id = r.Id,
                    Code = r.Code,
                    GuestName = r.PrimaryGuest.FullName,
                    PhoneNumber = r.PrimaryGuest.PhoneNumber,
                    CheckInDate = r.CheckInDate,
                    HoldUntil = holdUntil,
                    RoomSummary = string.Join(", ", r.Rooms.Select(rr => rr.Room != null ? rr.Room.RoomNumber : rr.RoomType.Code)),
                    DepositPaid = 0m,
                    HoursOverdue = (now - holdUntil).TotalHours
                };
            })
            .Where(x => x.HoursOverdue > 0)
            .ToList();

        return new NoShowListViewModel { Items = items };
    }

    public async Task<ServiceResult> MarkNoShowAsync(int id, int employeeId)
    {
        var r = await _db.Reservations
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (r is null || r.Status != ReservationStatus.Confirmed)
        {
            return ServiceResult.Fail("Chỉ đánh dấu No-show cho đơn Đã xác nhận.");
        }

        var forfeited = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => d.Amount);

        r.Status = ReservationStatus.NoShow;
        r.CancellationFee = forfeited;
        r.CancelledAt = DateTime.Now;
        r.CancelledBy = employeeId;
        r.CancellationReason = "No-show — quá hạn giữ chỗ";

        foreach (var d in r.Deposits.Where(d => d.Status == DepositStatus.Held))
        {
            d.Status = DepositStatus.Forfeited;
        }

        _audit.Log("MarkNoShow", nameof(Reservation), r.Id.ToString(),
            newValue: $"Thu 100% cọc {forfeited:N0} ₫");

        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: $"Đã đánh dấu No-show đơn {r.Code}. Thu {forfeited:N0} ₫ tiền cọc.");
    }

    public async Task<ServiceResult> ExtendHoldAsync(int id, int hours, int employeeId)
    {
        if (hours <= 0)
        {
            return ServiceResult.Fail("Số giờ gia hạn phải lớn hơn 0.");
        }

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || r.Status != ReservationStatus.Confirmed)
        {
            return ServiceResult.Fail("Chỉ gia hạn giữ chỗ cho đơn Đã xác nhận.");
        }

        var holdHour = await _settings.GetIntAsync(SystemSettingKeys.HoldUntilHour);
        var current = r.HoldUntil ?? r.CheckInDate.Date.AddHours(holdHour);
        r.HoldUntil = current.AddHours(hours);

        _audit.Log("ExtendHold", nameof(Reservation), r.Id.ToString(),
            newValue: $"Gia hạn giữ chỗ tới {r.HoldUntil:dd/MM/yyyy HH:mm}");

        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: $"Đã gia hạn giữ chỗ đơn {r.Code} thêm {hours} giờ.");
    }

    // ---------- Options & helpers ----------

    public async Task FillFormOptionsAsync(ReservationFormViewModel form)
    {
        form.GuestOptions = await _db.Guests.AsNoTracking()
            .OrderBy(g => g.FullName)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = $"{g.FullName} — {(g.IdNumber == "" ? "chưa có giấy tờ" : g.IdNumber)} — {g.PhoneNumber}"
            })
            .ToListAsync();

        form.RoomTypeOptions = await RoomTypeOptionsAsync();

        form.AllRooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive && r.Status != RoomStatus.OutOfService)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new RoomPickerItem
            {
                RoomId = r.Id,
                RoomTypeId = r.RoomTypeId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor
            })
            .ToListAsync();

        form.RoomTypePrices = await _db.RoomTypes.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.BasePricePerNight);

        if (form.PrimaryGuestId > 0)
        {
            var guest = await _db.Guests.AsNoTracking()
                .Where(g => g.Id == form.PrimaryGuestId)
                .Select(g => new { g.FullName, g.IsBlacklisted })
                .FirstOrDefaultAsync();
            form.PrimaryGuestName = guest?.FullName;
            form.PrimaryGuestBlacklisted = guest?.IsBlacklisted ?? false;
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> RoomTypeOptionsAsync()
        => await _db.RoomTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.Code} — {t.Name} ({t.BasePricePerNight:N0} ₫/đêm)"
            })
            .ToListAsync();

    private sealed class PreparedRooms
    {
        public ServiceResult Result { get; init; } = ServiceResult.Ok();
        public List<ReservationRoom> Rooms { get; init; } = new();
        public decimal EstimatedTotal { get; init; }
    }

    private async Task<PreparedRooms> PrepareRoomsAsync(ReservationFormViewModel form, bool isAdmin, int? excludeReservationId)
    {
        if (form.PrimaryGuestId <= 0)
        {
            return new PreparedRooms { Result = ServiceResult.Fail("Vui lòng chọn khách đứng tên.", nameof(form.PrimaryGuestId)) };
        }

        if (form.CheckOutDate.Date <= form.CheckInDate.Date)
        {
            return new PreparedRooms { Result = ServiceResult.Fail("Ngày đi phải sau ngày đến.", nameof(form.CheckOutDate)) };
        }

        var lines = (form.Rooms ?? new List<ReservationRoomInput>())
            .Where(l => l.RoomTypeId > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return new PreparedRooms { Result = ServiceResult.Fail("Đơn phải có ít nhất một dòng phòng.") };
        }

        var checkIn = form.CheckInDate.Date;
        var checkOut = form.CheckOutDate.Date;
        var nights = _pricing.CountNights(checkIn, checkOut);

        var typeIds = lines.Select(l => l.RoomTypeId).Distinct().ToList();
        var types = await _db.RoomTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var pickedRoomIds = new HashSet<int>();
        var entities = new List<ReservationRoom>();
        decimal estimatedTotal = 0m;

        foreach (var line in lines)
        {
            if (!types.TryGetValue(line.RoomTypeId, out var type))
            {
                return new PreparedRooms { Result = ServiceResult.Fail("Loại phòng không hợp lệ.") };
            }

            var guests = line.Adults + line.Children;
            if (line.Adults < 1)
            {
                return new PreparedRooms { Result = ServiceResult.Fail($"Dòng {type.Code}: cần ít nhất 1 người lớn.") };
            }

            if (guests > type.MaxCapacity)
            {
                return new PreparedRooms { Result = ServiceResult.Fail(
                    $"Dòng {type.Code}: {guests} khách vượt sức chứa tối đa ({type.MaxCapacity}).") };
            }

            if (line.RoomId is not null)
            {
                if (!pickedRoomIds.Add(line.RoomId.Value))
                {
                    return new PreparedRooms { Result = ServiceResult.Fail("Một phòng được chọn cho hai dòng trong cùng đơn.") };
                }

                var free = await _availability.IsRoomAvailableAsync(
                    line.RoomId.Value, checkIn, checkOut, excludeReservationId);
                if (!free)
                {
                    var roomNumber = await _db.Rooms.Where(r => r.Id == line.RoomId).Select(r => r.RoomNumber).FirstOrDefaultAsync();
                    return new PreparedRooms { Result = ServiceResult.Fail(
                        $"Phòng {roomNumber} đã có lịch trùng trong khoảng ngày này.") };
                }
            }

            // Giá đêm: chỉ Admin được sửa tay; lễ tân dùng giá niêm yết của loại phòng.
            var price = isAdmin && line.PricePerNight > 0 ? line.PricePerNight : type.BasePricePerNight;

            var extraGuest = _pricing.ExtraGuestSurcharge(guests, type.StandardCapacity, type.ExtraGuestFeePerNight, nights);
            estimatedTotal += price * nights + (extraGuest?.Amount ?? 0m);

            entities.Add(new ReservationRoom
            {
                RoomTypeId = type.Id,
                RoomId = line.RoomId,
                Adults = line.Adults,
                Children = line.Children,
                PricePerNight = price
            });
        }

        return new PreparedRooms
        {
            Result = ServiceResult.Ok(),
            Rooms = entities,
            EstimatedTotal = estimatedTotal
        };
    }

    private async Task<decimal> SuggestedDepositAsync(Reservation r)
    {
        var nights = await _settings.GetIntAsync(SystemSettingKeys.DepositNights);
        if (nights <= 0)
        {
            nights = 1;
        }

        // Mức cọc đề xuất = tiền của "nights" đêm đầu trên tổng dự kiến (BR-05, mặc định 1 đêm).
        return r.Nights > 0 ? r.EstimatedTotal / r.Nights * nights : r.EstimatedTotal;
    }

    private async Task<DateTime> ComputeHoldUntilAsync(DateTime checkInDate)
    {
        var holdHour = await _settings.GetIntAsync(SystemSettingKeys.HoldUntilHour);
        return checkInDate.Date.AddHours(holdHour);
    }

    private sealed record CancelNumbers(decimal DepositPaid, decimal Fee, decimal Refund, double HoursBeforeArrival, string Policy);

    private async Task<CancelNumbers> ComputeCancelNumbersAsync(Reservation r, bool waive)
    {
        var depositPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => d.Amount);
        var settings = await _settings.GetPricingSettingsAsync();

        var arrival = r.CheckInDate.Date.Add(settings.StandardCheckInTime.ToTimeSpan());
        var hours = (arrival - DateTime.Now).TotalHours;

        var fee = waive ? 0m : _pricing.CancellationFee(depositPaid, hours, settings);
        var refund = depositPaid - fee;

        var policy = hours >= 48
            ? "Hủy trước 48 giờ — hoàn 100% cọc"
            : hours >= 24
                ? "Hủy trong 24–48 giờ — thu 50% cọc"
                : "Hủy dưới 24 giờ — thu 100% cọc";

        return new CancelNumbers(depositPaid, fee, refund, hours, policy);
    }

    private async Task FillCancelNumbersAsync(CancelReservationViewModel vm, Reservation r, bool waiveFee)
    {
        var numbers = await ComputeCancelNumbersAsync(r, waiveFee);
        vm.DepositPaid = numbers.DepositPaid;
        vm.CancellationFee = numbers.Fee;
        vm.RefundAmount = numbers.Refund;
        vm.HoursBeforeArrival = numbers.HoursBeforeArrival;
        vm.PolicyDescription = numbers.Policy;
    }

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ---------- SCR-C03 ----------

    public async Task<RoomChartViewModel> BuildRoomChartAsync(DateTime? from, int days)
    {
        var start = (from ?? DateTime.Now).Date;

        var truncated = days > RoomChartViewModel.MaxDays;
        if (truncated)
        {
            days = RoomChartViewModel.MaxDays;
        }

        if (days < 1)
        {
            days = 14;
        }

        var endExclusive = start.AddDays(days);

        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .Select(r => new
            {
                r.Id,
                r.RoomNumber,
                r.Floor,
                r.Status,
                RoomTypeName = r.RoomType.Name
            })
            .ToListAsync();

        // Đơn đã giữ phòng cụ thể. Đơn hủy / no-show / đã trả phòng không còn chiếm chỗ.
        var booked = await _db.ReservationRooms.AsNoTracking()
            .Where(rr => rr.RoomId != null
                && rr.Reservation.CheckInDate < endExclusive
                && rr.Reservation.CheckOutDate > start
                && (rr.Reservation.Status == ReservationStatus.Confirmed
                    || rr.Reservation.Status == ReservationStatus.Draft))
            .Select(rr => new
            {
                RoomId = rr.RoomId!.Value,
                rr.ReservationId,
                rr.Reservation.Code,
                GuestName = rr.Reservation.PrimaryGuest.FullName,
                Start = rr.Reservation.CheckInDate,
                End = rr.Reservation.CheckOutDate,
                Guests = rr.Adults + rr.Children,
                rr.Reservation.Status
            })
            .ToListAsync();

        // Khách đang ở. Chưa trả phòng thì lấy ngày đi dự kiến làm mốc kết thúc.
        var stays = await _db.Stays.AsNoTracking()
            .Where(s => s.ActualCheckIn < endExclusive
                && (s.ActualCheckOut ?? s.ExpectedCheckOut) > start)
            .Select(s => new
            {
                s.RoomId,
                s.ReservationId,
                Code = s.Reservation != null ? s.Reservation.Code : null,
                GuestName = s.PrimaryGuest.FullName,
                Start = s.ActualCheckIn,
                End = s.ActualCheckOut ?? s.ExpectedCheckOut,
                Guests = s.Guests.Count
            })
            .ToListAsync();

        var dates = Enumerable.Range(0, days).Select(i => start.AddDays(i)).ToList();
        var rows = new List<RoomChartRow>(rooms.Count);

        foreach (var room in rooms)
        {
            // Dựng từng đêm trước rồi mới gộp dải: gộp thẳng trong lúc duyệt sẽ phải xử lý
            // riêng trường hợp hai đơn khác nhau nằm sát nhau, rất dễ dính thành một dải sai.
            var cells = new RoomChartSegment[days];

            for (var i = 0; i < days; i++)
            {
                var day = dates[i];

                // Một đêm bị chiếm khi lượt ở bắt đầu trước hoặc trong ngày đó và kết thúc sau ngày đó.
                var stay = stays.FirstOrDefault(s => s.RoomId == room.Id
                    && s.Start.Date <= day && s.End.Date > day);

                if (stay is not null)
                {
                    cells[i] = new RoomChartSegment
                    {
                        Kind = RoomChartCellKind.Occupied,
                        Date = day,
                        ReservationId = stay.ReservationId,
                        ReservationCode = stay.Code,
                        GuestName = stay.GuestName,
                        Guests = stay.Guests,
                        StatusLabel = "Đang ở"
                    };
                    continue;
                }

                var book = booked.FirstOrDefault(b => b.RoomId == room.Id
                    && b.Start.Date <= day && b.End.Date > day);

                if (book is not null)
                {
                    cells[i] = new RoomChartSegment
                    {
                        Kind = RoomChartCellKind.Reserved,
                        Date = day,
                        ReservationId = book.ReservationId,
                        ReservationCode = book.Code,
                        GuestName = book.GuestName,
                        Guests = book.Guests,
                        StatusLabel = book.Status.ToDisplayName()
                    };
                    continue;
                }

                // Bảo trì / ngừng khai thác là trạng thái hiện tại của phòng, dữ liệu không có
                // mốc thời gian, nên phủ toàn kỳ: sơ đồ thà báo thừa còn hơn mời bán phòng đang hỏng.
                var blocked = room.Status is RoomStatus.Maintenance or RoomStatus.OutOfService;

                cells[i] = new RoomChartSegment
                {
                    Kind = blocked ? RoomChartCellKind.Blocked : RoomChartCellKind.Free,
                    Date = day,
                    StatusLabel = blocked ? room.Status.ToDisplayName() : null
                };
            }

            rows.Add(new RoomChartRow
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                RoomTypeName = room.RoomTypeName,
                CurrentStatus = room.Status,
                Segments = MergeSegments(cells)
            });
        }

        return new RoomChartViewModel
        {
            From = start,
            Days = days,
            Dates = dates,
            Rows = rows,
            Truncated = truncated
        };
    }

    /// <summary>
    /// Gộp các đêm liền nhau thuộc cùng một đơn thành một dải — SCR-C03.
    /// Hai đơn khác nhau nằm sát nhau vẫn phải là hai dải, nếu không sơ đồ sẽ vẽ ra một
    /// khoảng đặt phòng liền mạch không có thật.
    /// </summary>
    private static List<RoomChartSegment> MergeSegments(RoomChartSegment[] cells)
    {
        var merged = new List<RoomChartSegment>();

        foreach (var cell in cells)
        {
            var last = merged.Count > 0 ? merged[merged.Count - 1] : null;

            var sameRun = last is not null
                && last.Kind == cell.Kind
                && last.ReservationId == cell.ReservationId;

            if (sameRun)
            {
                last!.Span++;
                continue;
            }

            merged.Add(new RoomChartSegment
            {
                Kind = cell.Kind,
                Date = cell.Date,
                Span = 1,
                ReservationId = cell.ReservationId,
                ReservationCode = cell.ReservationCode,
                GuestName = cell.GuestName,
                Guests = cell.Guests,
                StatusLabel = cell.StatusLabel
            });
        }

        foreach (var seg in merged)
        {
            if (seg.Kind == RoomChartCellKind.Free)
            {
                seg.Tooltip = null;
                continue;
            }

            if (seg.Kind == RoomChartCellKind.Blocked)
            {
                seg.Tooltip = seg.StatusLabel;
                continue;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(seg.ReservationCode))
            {
                parts.Add(seg.ReservationCode!);
            }

            if (!string.IsNullOrWhiteSpace(seg.GuestName))
            {
                parts.Add(seg.GuestName!);
            }

            if (seg.Guests > 0)
            {
                parts.Add(seg.Guests + " khách");
            }

            if (!string.IsNullOrWhiteSpace(seg.StatusLabel))
            {
                parts.Add(seg.StatusLabel!);
            }

            parts.Add(seg.Span + " đêm");
            seg.Tooltip = string.Join(" · ", parts);
        }

        return merged;
    }

    // ---------- Giải thích lỗi ----------
    // Chỉ chạy khi không mở được màn hình, để nói đúng lý do thay vì trả 404 trắng.

    public async Task<(ReservationStatus? Status, string? Code)> GetStatusAsync(int reservationId)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Where(x => x.Id == reservationId)
            .Select(x => new { x.Status, x.Code })
            .FirstOrDefaultAsync();

        return r is null ? (null, null) : (r.Status, r.Code);
    }
}
