using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Lễ tân — nhóm D (SCR-D01…D08), FR-D01…D08, BR-01/02/03/06/08/09.
/// Check-in, check-out, đổi phòng, gia hạn đều chạy trong transaction (NFR-03) và dùng lại
/// hạ tầng SP0 + <see cref="IBillingService"/> để tính folio.
/// </summary>
public interface IFrontDeskService
{
    Task<FrontDeskDashboardViewModel> BuildDashboardAsync();

    Task<CheckInViewModel?> BuildCheckInAsync(int reservationId);
    Task<ServiceResult> CheckInAsync(CheckInViewModel form, int employeeId);


    Task<StayDetailViewModel?> GetStayAsync(int stayId);

    Task<AddGuestViewModel?> BuildAddGuestAsync(int stayId);
    Task<ServiceResult> AddGuestAsync(AddGuestViewModel form, int employeeId);

    Task<ChangeRoomViewModel?> BuildChangeRoomAsync(int stayId, bool isAdmin);
    Task<ServiceResult> ChangeRoomAsync(ChangeRoomViewModel form, int employeeId, bool isAdmin);

    Task<ExtendStayViewModel?> BuildExtendAsync(int stayId, bool isAdmin);
    Task<ServiceResult> ExtendAsync(ExtendStayViewModel form, int employeeId, bool isAdmin);

    Task<CheckOutViewModel?> BuildCheckOutAsync(int stayId, bool isAdmin);
    Task<ServiceResult> CheckOutAsync(CheckOutViewModel form, int employeeId, bool isAdmin);
}

/// <inheritdoc />
public class FrontDeskService : IFrontDeskService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly IAvailabilityService _availability;
    private readonly IPricingService _pricing;
    private readonly INumberSequenceService _numbers;
    private readonly ITransactionRunner _tx;
    private readonly IShiftService _shifts;
    private readonly ISettingsReader _settings;
    private readonly IBillingService _billing;

    public FrontDeskService(
        HotelDbContext db, IAuditService audit, IAvailabilityService availability, IPricingService pricing,
        INumberSequenceService numbers, ITransactionRunner tx, IShiftService shifts, ISettingsReader settings,
        IBillingService billing)
    {
        _db = db;
        _audit = audit;
        _availability = availability;
        _pricing = pricing;
        _numbers = numbers;
        _tx = tx;
        _shifts = shifts;
        _settings = settings;
        _billing = billing;
    }

    // ---------- SCR-D01 ----------

    public async Task<FrontDeskDashboardViewModel> BuildDashboardAsync()
    {
        var today = DateTime.Now.Date;
        var settings = await _settings.GetPricingSettingsAsync();
        var noonToday = today.Add(settings.StandardCheckOutTime.ToTimeSpan());

        var arrivals = await _db.Reservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Confirmed && r.CheckInDate <= today)
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .Include(r => r.Deposits)
            .OrderBy(r => r.CheckInDate)
            .Select(r => new ArrivalItem
            {
                ReservationId = r.Id,
                Code = r.Code,
                GuestName = r.PrimaryGuest.FullName,
                PhoneNumber = r.PrimaryGuest.PhoneNumber,
                RoomTypeSummary = string.Join(", ", r.Rooms.Select(rr => rr.RoomType.Code)),
                Guests = r.Rooms.Sum(rr => rr.Adults + rr.Children),
                DepositPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => (decimal?)d.Amount) ?? 0m,
                CanCheckIn = true
            })
            .ToListAsync();

        var stays = await _db.Stays.AsNoTracking()
            .Where(s => s.Status == StayStatus.CheckedIn)
            .Include(s => s.Room)
            .Include(s => s.PrimaryGuest)
            .ToListAsync();

        var departures = new List<DepartureItem>();
        var inHouse = new List<InHouseItem>();
        foreach (var s in stays)
        {
            inHouse.Add(new InHouseItem
            {
                StayId = s.Id,
                RoomNumber = s.Room.RoomNumber,
                GuestName = s.PrimaryGuest.FullName,
                ActualCheckIn = s.ActualCheckIn,
                ExpectedCheckOut = s.ExpectedCheckOut
            });

            if (s.ExpectedCheckOut.Date <= today)
            {
                var summary = await _billing.ComputeFolioSummaryAsync(s.Id);
                departures.Add(new DepartureItem
                {
                    StayId = s.Id,
                    RoomNumber = s.Room.RoomNumber,
                    GuestName = s.PrimaryGuest.FullName,
                    ExpectedCheckOut = s.ExpectedCheckOut,
                    Nights = s.Nights,
                    BalanceDue = summary?.BalanceDue ?? 0m,
                    IsInspected = s.IsInspected,
                    OverdueNoon = DateTime.Now > noonToday
                });
            }
        }

        // Chỉ cần trạng thái để đếm cho thanh chỉ số đầu trang. Tab "Phòng" đã bỏ
        // (trùng SCR-E01 và không còn thao tác nào), nên không kéo về số phòng và tầng nữa.
        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Status)
            .ToListAsync();

        // Đã check-in hôm nay
        var checkedInToday = await _db.Stays.AsNoTracking()
            .Include(s => s.Room)
            .Include(s => s.PrimaryGuest)
            .Where(s => s.ActualCheckIn.Date == today)
            .OrderBy(s => s.ActualCheckIn)
            .Select(s => new CheckedInTodayItem
            {
                StayId = s.Id,
                RoomNumber = s.Room.RoomNumber,
                GuestName = s.PrimaryGuest.FullName,
                ActualCheckIn = s.ActualCheckIn,
                ExpectedCheckOut = s.ExpectedCheckOut
            })
            .ToListAsync();

        // Đã check-out hôm nay
        var checkedOutToday = await _db.Stays.AsNoTracking()
            .Include(s => s.Room)
            .Include(s => s.PrimaryGuest)
            .Where(s => s.Status == StayStatus.CheckedOut && s.ActualCheckOut != null && s.ActualCheckOut.Value.Date == today)
            .OrderBy(s => s.ActualCheckOut)
            .Select(s => new CheckedOutTodayItem
            {
                StayId = s.Id,
                RoomNumber = s.Room.RoomNumber,
                GuestName = s.PrimaryGuest.FullName,
                ActualCheckIn = s.ActualCheckIn,
                ActualCheckOut = s.ActualCheckOut!.Value,
                Nights = s.Nights
            })
            .ToListAsync();

        return new FrontDeskDashboardViewModel
        {
            Arrivals = arrivals,
            Departures = departures.OrderBy(d => d.RoomNumber).ToList(),
            InHouse = inHouse.OrderBy(i => i.RoomNumber).ToList(),
            CheckedInToday = checkedInToday,
            CheckedOutToday = checkedOutToday,
            AvailableCount = rooms.Count(s => s == RoomStatus.Available),
            OccupiedCount = rooms.Count(s => s == RoomStatus.Occupied),
            DirtyCount = rooms.Count(s => s == RoomStatus.Dirty),
            MaintenanceCount = rooms.Count(s => s == RoomStatus.Maintenance)
        };
    }

    // ---------- SCR-D02 ----------

    public async Task<CheckInViewModel?> BuildCheckInAsync(int reservationId)
    {
        var r = await _db.Reservations.AsNoTracking()
            .Include(x => x.PrimaryGuest)
            .Include(x => x.Rooms).ThenInclude(rr => rr.RoomType)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == reservationId);

        if (r is null || r.Status != ReservationStatus.Confirmed)
        {
            return null;
        }

        var vm = new CheckInViewModel
        {
            ReservationId = r.Id,
            Code = r.Code,
            GuestName = r.PrimaryGuest.FullName,
            IdNumber = r.PrimaryGuest.IdNumber,
            GuestBlacklisted = r.PrimaryGuest.IsBlacklisted,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Nights = r.Nights,
            DepositPaid = r.Deposits.Where(d => d.Status == DepositStatus.Held).Sum(d => d.Amount),
            Rooms = new List<CheckInRoomAssignment>()
        };

        foreach (var rr in r.Rooms)
        {
            var options = await AvailableRoomOptionsAsync(rr.RoomTypeId, r.CheckInDate, r.CheckOutDate, rr.RoomId);
            vm.Rooms.Add(new CheckInRoomAssignment
            {
                ReservationRoomId = rr.Id,
                RoomTypeId = rr.RoomTypeId,
                RoomTypeName = rr.RoomType.Name,
                StandardCapacity = rr.RoomType.StandardCapacity,
                MaxCapacity = rr.RoomType.MaxCapacity,
                Adults = rr.Adults,
                Children = rr.Children,
                PricePerNight = rr.PricePerNight,
                SelectedRoomId = rr.RoomId,
                AvailableRooms = options
            });
        }

        return vm;
    }

    public async Task<ServiceResult> CheckInAsync(CheckInViewModel form, int employeeId)
    {
        if (!form.IdVerified)
        {
            return ServiceResult.Fail("Phải tích \"Đã đối chiếu giấy tờ tùy thân\" trước khi check-in (BR-07).");
        }

        var reservation = await _db.Reservations
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .FirstOrDefaultAsync(r => r.Id == form.ReservationId);

        if (reservation is null || reservation.Status != ReservationStatus.Confirmed)
        {
            return ServiceResult.Fail("Đơn không ở trạng thái Đã xác nhận nên không check-in được.");
        }

        var guestBlacklisted = await _db.Guests.AsNoTracking()
            .Where(g => g.Id == reservation.PrimaryGuestId)
            .Select(g => g.IsBlacklisted)
            .FirstOrDefaultAsync();

        var settings = await _settings.GetPricingSettingsAsync();
        var nights = _pricing.CountNights(reservation.CheckInDate, reservation.CheckOutDate);
        var actualCheckIn = form.ActualCheckIn;

        // Kiểm tra phòng chọn trước khi mở transaction.
        var pickedRoomIds = new HashSet<int>();
        foreach (var line in form.Rooms)
        {
            if (line.SelectedRoomId is null)
            {
                return ServiceResult.Fail("Mỗi phòng của đơn phải được gán một phòng cụ thể.");
            }

            if (!pickedRoomIds.Add(line.SelectedRoomId.Value))
            {
                return ServiceResult.Fail("Một phòng được gán cho hai dòng.");
            }

            var guests = line.Adults + line.Children;
            var rr = reservation.Rooms.FirstOrDefault(x => x.Id == line.ReservationRoomId);
            if (rr is null)
            {
                return ServiceResult.Fail("Dòng phòng không thuộc đơn này.");
            }

            if (guests > rr.RoomType.MaxCapacity)
            {
                return ServiceResult.Fail($"{guests} khách vượt sức chứa tối đa của {rr.RoomType.Name}.");
            }

            var free = await _availability.IsRoomAvailableAsync(
                line.SelectedRoomId.Value, reservation.CheckInDate, reservation.CheckOutDate, reservation.Id);
            var roomOk = await _db.Rooms.AnyAsync(x => x.Id == line.SelectedRoomId
                && x.Status == RoomStatus.Available);
            if (!free || !roomOk)
            {
                var no = await _db.Rooms.Where(x => x.Id == line.SelectedRoomId).Select(x => x.RoomNumber).FirstOrDefaultAsync();
                return ServiceResult.Fail($"Phòng {no} không sẵn sàng (đã có khách hoặc chưa dọn). Chọn phòng khác.");
            }
        }

        await _tx.ExecuteAsync(async () =>
        {
            foreach (var line in form.Rooms)
            {
                var rr = reservation.Rooms.First(x => x.Id == line.ReservationRoomId);
                var room = await _db.Rooms.FirstAsync(x => x.Id == line.SelectedRoomId!.Value);

                var stay = new Stay
                {
                    ReservationId = reservation.Id,
                    RoomId = room.Id,
                    PrimaryGuestId = reservation.PrimaryGuestId,
                    ActualCheckIn = actualCheckIn,
                    ExpectedCheckOut = reservation.CheckOutDate,
                    PricePerNight = rr.PricePerNight,
                    Nights = nights,
                    Status = StayStatus.CheckedIn
                };
                _db.Stays.Add(stay);
                await _db.SaveChangesAsync();

                _db.StayGuests.Add(new StayGuest
                {
                    StayId = stay.Id,
                    GuestId = reservation.PrimaryGuestId,
                    IsPrimary = true
                });

                var folio = new Folio
                {
                    StayId = stay.Id,
                    Code = await _numbers.NextFolioCodeAsync(),
                    IsLocked = false
                };
                _db.Folios.Add(folio);
                await _db.SaveChangesAsync();

                AddRoomChargeLine(folio.Id, rr.RoomType.Name, rr.PricePerNight, nights, actualCheckIn);
                AddEarlyCheckInSurcharge(folio.Id, actualCheckIn, rr.PricePerNight, settings);
                AddExtraGuestSurcharge(folio.Id, line.Adults + line.Children, rr.RoomType, nights, actualCheckIn);

                room.Status = RoomStatus.Occupied;
            }

            reservation.Status = ReservationStatus.CheckedIn;
            _audit.Log("CheckIn", nameof(Reservation), reservation.Id.ToString(),
                newValue: $"Nhận phòng lúc {actualCheckIn:dd/MM/yyyy HH:mm}");

            // Cảnh báo khách hạn chế không tự chặn (SCR-B05) — quyết định là của con người.
            // Nhưng đã bấm tiếp thì phải để lại dấu vết, nếu không SCR-G04 không đếm được.
            if (guestBlacklisted)
            {
                _audit.Log(AuditActions.OverrideBlacklistWarning, nameof(Reservation),
                    reservation.Id.ToString(),
                    reason: "Check-in cho khách nằm trong danh sách hạn chế");
            }
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã check-in đơn {reservation.Code}.");
    }

    // ---------- SCR-D04 ----------

    public async Task<StayDetailViewModel?> GetStayAsync(int stayId)
    {
        var stay = await _db.Stays.AsNoTracking()
            .Include(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(s => s.PrimaryGuest)
            .Include(s => s.Guests).ThenInclude(g => g.Guest)
            .Include(s => s.RoomChanges)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay is null)
        {
            return null;
        }

        var summary = await _billing.ComputeFolioSummaryAsync(stayId) ?? new BillingFolioSummary();

        var history = stay.RoomChanges
            .OrderBy(c => c.ChangedAt)
            .Select(c => new StayHistoryLine { At = c.ChangedAt, Description = $"Đổi phòng: {c.Reason}" })
            .ToList();

        return new StayDetailViewModel
        {
            Stay = stay,
            RoomNumber = stay.Room.RoomNumber,
            RoomTypeName = stay.Room.RoomType.Name,
            PrimaryGuestName = stay.PrimaryGuest.FullName,
            Guests = stay.Guests.Select(g => g.Guest.FullName + (g.IsPrimary ? " (đứng tên)" : "")).ToList(),
            Summary = summary,
            History = history
        };
    }

    // ---------- SCR-D05 ----------

    public async Task<AddGuestViewModel?> BuildAddGuestAsync(int stayId)
    {
        var stay = await _db.Stays.AsNoTracking()
            .Include(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(s => s.Guests)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return null;
        }

        return new AddGuestViewModel
        {
            StayId = stayId,
            RoomNumber = stay.Room.RoomNumber,
            CurrentGuestCount = stay.Guests.Count,
            StandardCapacity = stay.Room.RoomType.StandardCapacity,
            MaxCapacity = stay.Room.RoomType.MaxCapacity,
            ExtraGuestFee = stay.Room.RoomType.ExtraGuestFeePerNight,
            RemainingNights = RemainingNights(stay)
        };
    }

    public async Task<ServiceResult> AddGuestAsync(AddGuestViewModel form, int employeeId)
    {
        var stay = await _db.Stays
            .Include(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(s => s.Guests)
            .FirstOrDefaultAsync(s => s.Id == form.StayId);

        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Không tìm thấy lượt lưu trú đang mở.");
        }

        if (string.IsNullOrWhiteSpace(form.FullName))
        {
            return ServiceResult.Fail("Vui lòng nhập họ tên.", nameof(form.FullName));
        }

        var newCount = stay.Guests.Count + 1;
        if (newCount > stay.Room.RoomType.MaxCapacity)
        {
            return ServiceResult.Fail($"Vượt sức chứa tối đa ({stay.Room.RoomType.MaxCapacity}). Cần đặt thêm phòng.");
        }

        var remainingNights = RemainingNights(stay);
        var warning = (string?)null;

        await _tx.ExecuteAsync(async () =>
        {
            var (guestId, guestWarning) = await ResolveGuestAsync(new Guest
            {
                FullName = form.FullName.Trim(),
                IdType = form.IdType,
                IdNumber = form.IdNumber?.Trim() ?? string.Empty,
                Nationality = string.IsNullOrWhiteSpace(form.Nationality) ? "Việt Nam" : form.Nationality.Trim()
            });

            warning = Join(warning, guestWarning);

            _db.StayGuests.Add(new StayGuest
            {
                StayId = stay.Id,
                GuestId = guestId,
                IsPrimary = false,
                IsChild = form.IsChild
            });

            // Vượt sức chứa chuẩn (không tính trẻ em) → thêm phụ thu cho số đêm còn lại (BR-03).
            var payingCount = stay.Guests.Count(g => !g.IsChild) + (form.IsChild ? 0 : 1);
            if (payingCount > stay.Room.RoomType.StandardCapacity && remainingNights > 0 && !form.IsChild)
            {
                var fee = stay.Room.RoomType.ExtraGuestFeePerNight * remainingNights;
                var folio = await _db.Folios.FirstAsync(f => f.StayId == stay.Id);
                _db.FolioItems.Add(new FolioItem
                {
                    FolioId = folio.Id,
                    ItemType = FolioItemType.Surcharge,
                    SurchargeType = SurchargeType.ExtraGuest,
                    Description = $"Thêm người × {remainingNights} đêm còn lại",
                    Quantity = remainingNights,
                    UnitPrice = stay.Room.RoomType.ExtraGuestFeePerNight,
                    Amount = fee,
                    ChargedAt = DateTime.Now
                });
                warning = Join(warning, $"Vượt sức chứa chuẩn — đã thêm phụ thu {fee:N0} ₫.");
            }

            _audit.Log("AddStayGuest", nameof(Stay), stay.Id.ToString(), newValue: form.FullName.Trim());
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(warning, "Đã thêm khách vào phòng.");
    }

    // ---------- SCR-D06 ----------

    public async Task<ChangeRoomViewModel?> BuildChangeRoomAsync(int stayId, bool isAdmin)
    {
        var stay = await _db.Stays.AsNoTracking()
            .Include(s => s.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return null;
        }

        var options = await AvailableRoomOptionsAsync(null, DateTime.Now, stay.ExpectedCheckOut, null, excludeStayId: stay.Id);

        return new ChangeRoomViewModel
        {
            StayId = stayId,
            CurrentRoomNumber = stay.Room.RoomNumber,
            CurrentRoomType = stay.Room.RoomType.Name,
            CurrentPricePerNight = stay.PricePerNight,
            IsAdmin = isAdmin,
            AvailableRooms = options
        };
    }

    public async Task<ServiceResult> ChangeRoomAsync(ChangeRoomViewModel form, int employeeId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(form.Reason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do đổi phòng (BR-09).", nameof(form.Reason));
        }

        var stay = await _db.Stays.Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == form.StayId);
        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Không tìm thấy lượt lưu trú đang mở.");
        }

        var newRoom = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == form.NewRoomId);
        if (newRoom is null || newRoom.Status != RoomStatus.Available || !newRoom.IsActive)
        {
            return ServiceResult.Fail("Phòng đích không sẵn sàng (phải Trống và đã dọn).");
        }

        var free = await _availability.IsRoomAvailableAsync(newRoom.Id, DateTime.Now, stay.ExpectedCheckOut, null, stay.Id);
        if (!free)
        {
            return ServiceResult.Fail("Phòng đích đã có lịch trùng trong khoảng còn lại.");
        }

        var oldRoom = stay.Room;
        var oldPrice = stay.PricePerNight;
        var newPrice = form.WaivePriceDifference && isAdmin ? oldPrice : newRoom.RoomType.BasePricePerNight;

        await _tx.ExecuteAsync(async () =>
        {
            _db.RoomChangeLogs.Add(new RoomChangeLog
            {
                StayId = stay.Id,
                FromRoomId = oldRoom.Id,
                ToRoomId = newRoom.Id,
                ChangedAt = DateTime.Now,
                Reason = form.Reason.Trim(),
                OldPricePerNight = oldPrice,
                NewPricePerNight = newPrice,
                PriceDifferenceWaived = form.WaivePriceDifference && isAdmin
            });

            // Chi phí phòng cũ tính đến thời điểm đổi giữ nguyên trên folio (BR-09).
            // Các đêm còn lại theo giá phòng mới: cập nhật giá/đêm của Stay cho các thao tác sau.
            stay.RoomId = newRoom.Id;
            stay.PricePerNight = newPrice;

            oldRoom.Status = RoomStatus.Dirty;
            newRoom.Status = RoomStatus.Occupied;

            _audit.Log("ChangeRoom", nameof(Stay), stay.Id.ToString(), reason: form.Reason.Trim(),
                oldValue: oldRoom.RoomNumber, newValue: newRoom.RoomNumber);
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã đổi sang phòng {newRoom.RoomNumber}.");
    }

    // ---------- SCR-D07 ----------

    public async Task<ExtendStayViewModel?> BuildExtendAsync(int stayId, bool isAdmin)
    {
        var stay = await _db.Stays.AsNoTracking().Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return null;
        }

        return new ExtendStayViewModel
        {
            StayId = stayId,
            RoomNumber = stay.Room.RoomNumber,
            CurrentCheckOut = stay.ExpectedCheckOut,
            PricePerNight = stay.PricePerNight,
            ExtraNightPrice = stay.PricePerNight,
            NewCheckOut = stay.ExpectedCheckOut.AddDays(1),
            IsAdmin = isAdmin
        };
    }

    public async Task<ServiceResult> ExtendAsync(ExtendStayViewModel form, int employeeId, bool isAdmin)
    {
        var stay = await _db.Stays.Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == form.StayId);
        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Không tìm thấy lượt lưu trú đang mở.");
        }

        if (form.NewCheckOut.Date <= stay.ExpectedCheckOut.Date)
        {
            return ServiceResult.Fail("Ngày đi mới phải muộn hơn ngày đi hiện tại.", nameof(form.NewCheckOut));
        }

        var free = await _availability.IsRoomAvailableAsync(
            stay.RoomId, stay.ExpectedCheckOut, form.NewCheckOut.Date, null, stay.Id);
        if (!free)
        {
            return ServiceResult.Fail("Phòng đã có đơn khác trong các đêm thêm — cần đổi phòng.");
        }

        var extraNights = _pricing.CountNights(stay.ExpectedCheckOut, form.NewCheckOut);
        var price = isAdmin && form.ExtraNightPrice > 0 ? form.ExtraNightPrice : stay.PricePerNight;

        await _tx.ExecuteAsync(async () =>
        {
            var folio = await _db.Folios.FirstAsync(f => f.StayId == stay.Id);
            _db.FolioItems.Add(new FolioItem
            {
                FolioId = folio.Id,
                ItemType = FolioItemType.Room,
                Description = $"{stay.Room.RoomType?.Name ?? "Phòng"} × {extraNights} đêm gia hạn",
                Quantity = extraNights,
                UnitPrice = price,
                Amount = price * extraNights,
                ChargedAt = DateTime.Now
            });

            stay.ExpectedCheckOut = form.NewCheckOut.Date;
            stay.Nights += extraNights;

            _audit.Log("ExtendStay", nameof(Stay), stay.Id.ToString(),
                newValue: $"Gia hạn tới {form.NewCheckOut:dd/MM/yyyy} (+{extraNights} đêm)");
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã gia hạn thêm {extraNights} đêm.");
    }

    // ---------- SCR-D08 ----------

    public async Task<CheckOutViewModel?> BuildCheckOutAsync(int stayId, bool isAdmin)
    {
        var stay = await _db.Stays.AsNoTracking()
            .Include(s => s.Room)
            .Include(s => s.PrimaryGuest)
            .Include(s => s.Folio)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay is null || stay.Status != StayStatus.CheckedIn)
        {
            return null;
        }

        var settings = await _settings.GetPricingSettingsAsync();
        var now = DateTime.Now;

        // Phụ thu trả trễ CHỈ áp dụng khi trả đúng ngày hoặc sau ngày ExpectedCheckOut.
        // Nếu trả SỚM hơn ngày dự kiến thì không có phụ thu dù giờ có muộn.
        SurchargeLine? late = null;
        if (now.Date >= stay.ExpectedCheckOut.Date)
        {
            late = _pricing.LateCheckOutSurcharge(TimeOnly.FromDateTime(now), stay.PricePerNight, settings);
        }

        var summary = await _billing.ComputeFolioSummaryAsync(stayId) ?? new BillingFolioSummary();

        return new CheckOutViewModel
        {
            StayId = stayId,
            RoomNumber = stay.Room.RoomNumber,
            GuestName = stay.PrimaryGuest.FullName,
            ActualCheckIn = stay.ActualCheckIn,
            ExpectedCheckOut = stay.ExpectedCheckOut,
            PlannedNights = stay.Nights,
            IsAdmin = isAdmin,
            IsInspected = stay.IsInspected,
            IsFolioLocked = stay.Folio?.IsLocked ?? false,
            ActualCheckOut = now,
            LateSurchargeDescription = late?.Description,
            LateSurchargeAmount = late?.Amount ?? 0m,
            ExtraNightsFromLate = late?.ExtraNights ?? 0,
            Summary = summary
        };
    }

    public async Task<ServiceResult> CheckOutAsync(CheckOutViewModel form, int employeeId, bool isAdmin)
    {
        var stay = await _db.Stays.Include(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(s => s.Folio).ThenInclude(f => f!.Items)
            .FirstOrDefaultAsync(s => s.Id == form.StayId);

        if (stay is null || stay.Status != StayStatus.CheckedIn || stay.Folio is null)
        {
            return ServiceResult.Fail("Không tìm thấy lượt lưu trú đang mở.");
        }

        if (!stay.IsInspected)
        {
            return ServiceResult.Fail("Chưa kiểm phòng & minibar (BR-08). Vào \"Kiểm phòng\" trước.");
        }

        var settings = await _settings.GetPricingSettingsAsync();
        var actualCheckOut = form.ActualCheckOut;

        await _tx.ExecuteAsync(async () =>
        {
            // Trả sớm: tính lại theo số đêm thực ở nếu chỉ có 1 dòng tiền phòng.
            var actualNights = _pricing.CountNights(stay.ActualCheckIn, actualCheckOut);
            var roomLines = stay.Folio!.Items.Where(i => i.ItemType == FolioItemType.Room && !i.IsVoided).ToList();
            if (actualNights < stay.Nights && roomLines.Count == 1)
            {
                var rl = roomLines[0];
                rl.Quantity = actualNights;
                rl.Amount = rl.UnitPrice * actualNights;
                stay.Nights = actualNights;
            }

            // Phụ thu trả trễ (BR-03): CHỈ áp dụng khi trả đúng ngày hoặc sau ngày ExpectedCheckOut.
            // Trả SỚM hơn ngày dự kiến → không phụ thu dù giờ có muộn.
            if (!(form.WaiveLateSurcharge && isAdmin) && actualCheckOut.Date >= stay.ExpectedCheckOut.Date)
            {
                var late = _pricing.LateCheckOutSurcharge(TimeOnly.FromDateTime(actualCheckOut), stay.PricePerNight, settings);
                if (late is not null)
                {
                    _db.FolioItems.Add(new FolioItem
                    {
                        FolioId = stay.Folio.Id,
                        ItemType = late.ExtraNights > 0 ? FolioItemType.Room : FolioItemType.Surcharge,
                        SurchargeType = late.ExtraNights > 0 ? SurchargeType.None : SurchargeType.LateCheckOut,
                        Description = late.Description,
                        Quantity = 1,
                        UnitPrice = late.Amount,
                        Amount = late.Amount,
                        ChargedAt = DateTime.Now
                    });
                }
            }

            stay.ActualCheckOut = actualCheckOut;
            stay.Folio.IsLocked = true;
            stay.Folio.LockedAt = DateTime.Now;
            stay.Folio.LockedBy = employeeId;

            _audit.Log("CheckOutLockFolio", nameof(Stay), stay.Id.ToString(),
                newValue: $"Trả phòng {actualCheckOut:dd/MM/yyyy HH:mm}, khóa folio");
            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: "Đã chốt giờ trả và khóa folio. Tiếp tục thanh toán.");
    }

    // ---------- Helpers ----------

    private void AddRoomChargeLine(int folioId, string roomTypeName, decimal price, int nights, DateTime chargedAt)
        => _db.FolioItems.Add(new FolioItem
        {
            FolioId = folioId,
            ItemType = FolioItemType.Room,
            Description = $"{roomTypeName} × {nights} đêm",
            Quantity = nights,
            UnitPrice = price,
            Amount = price * nights,
            ChargedAt = chargedAt
        });

    private void AddEarlyCheckInSurcharge(int folioId, DateTime actualCheckIn, decimal price, PricingSettings settings)
    {
        var early = _pricing.EarlyCheckInSurcharge(TimeOnly.FromDateTime(actualCheckIn), price, settings);
        if (early is not null)
        {
            _db.FolioItems.Add(new FolioItem
            {
                FolioId = folioId,
                ItemType = FolioItemType.Surcharge,
                SurchargeType = SurchargeType.EarlyCheckIn,
                Description = early.Description,
                Quantity = 1,
                UnitPrice = early.Amount,
                Amount = early.Amount,
                ChargedAt = actualCheckIn
            });
        }
    }

    private void AddExtraGuestSurcharge(int folioId, int guests, RoomType type, int nights, DateTime chargedAt)
    {
        var extra = _pricing.ExtraGuestSurcharge(guests, type.StandardCapacity, type.ExtraGuestFeePerNight, nights);
        if (extra is not null)
        {
            _db.FolioItems.Add(new FolioItem
            {
                FolioId = folioId,
                ItemType = FolioItemType.Surcharge,
                SurchargeType = SurchargeType.ExtraGuest,
                Description = extra.Description,
                Quantity = nights,
                UnitPrice = type.ExtraGuestFeePerNight,
                Amount = extra.Amount,
                ChargedAt = chargedAt
            });
        }
    }

    private static int RemainingNights(Stay stay)
    {
        var remaining = (stay.ExpectedCheckOut.Date - DateTime.Now.Date).Days;
        return remaining < 0 ? 0 : remaining;
    }

    private async Task<IReadOnlyList<SelectListItem>> AvailableRoomOptionsAsync(
        int? roomTypeId, DateTime checkIn, DateTime checkOut, int? currentRoomId, int? excludeStayId = null)
    {
        var rooms = await _availability.GetAvailableRoomsAsync(checkIn, checkOut, roomTypeId);
        var list = rooms
            .Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"{r.RoomNumber} — {r.RoomTypeName} (tầng {r.Floor})",
                Selected = currentRoomId == r.RoomId
            })
            .ToList();

        // Giữ lại phòng đang chọn của đơn (đã gán sẵn) kể cả khi nó không nằm trong danh sách trống.
        if (currentRoomId is not null && list.All(x => x.Value != currentRoomId.ToString()))
        {
            var cur = await _db.Rooms.AsNoTracking().Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == currentRoomId);
            if (cur is not null)
            {
                list.Insert(0, new SelectListItem
                {
                    Value = cur.Id.ToString(),
                    Text = $"{cur.RoomNumber} — {cur.RoomType.Name} (đã gán)",
                    Selected = true
                });
            }
        }

        return list;
    }

    /// <summary>
    /// Lấy hồ sơ khách theo số giấy tờ, tạo mới nếu chưa có — SCR-D03, SCR-D05.
    ///
    /// Số giấy tờ là duy nhất toàn hệ thống (IX_Guests_IdNumber, lọc bỏ chuỗi rỗng). Hai màn
    /// hình này trước đây tạo thẳng Guest mới, nên lễ tân gõ đúng CCCD của một khách đã có hồ sơ
    /// là vỡ unique index và văng ra trang lỗi giữa lúc khách đứng chờ ở quầy.
    ///
    /// Khách cũ quay lại là chuyện bình thường — SCR-D03 mô tả ô nhập này là "tìm nhanh theo
    /// CCCD/SĐT (khách cũ quay lại) hoặc nhập mới" — nên dùng lại hồ sơ đang có thay vì bắt
    /// người dùng quay ra chọn từ danh sách.
    /// </summary>
    private async Task<(int GuestId, string? Warning)> ResolveGuestAsync(Guest draft)
    {
        // Chuẩn hóa giống GuestService để hai đường tạo khách không sinh ra hai dạng viết khác nhau
        // của cùng một số giấy tờ.
        draft.IdNumber = draft.IdNumber.Trim().ToUpperInvariant();

        if (draft.IdNumber.Length > 0)
        {
            var existing = await _db.Guests.AsNoTracking()
                .Where(g => g.IdNumber == draft.IdNumber)
                .Select(g => new { g.Id, g.FullName })
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                // Tên gõ vào khác tên đang lưu thì phải nói ra: rất có thể gõ nhầm số giấy tờ
                // của người khác, mà im lặng gắn lượt ở vào hồ sơ sai thì sau này không lần ra.
                var warning = string.Equals(existing.FullName, draft.FullName, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : $"Số giấy tờ {draft.IdNumber} đã thuộc hồ sơ \"{existing.FullName}\". "
                      + "Đã dùng hồ sơ có sẵn thay vì tạo mới — kiểm tra lại nếu đây không phải cùng một người.";

                return (existing.Id, warning);
            }
        }

        _db.Guests.Add(draft);
        await _db.SaveChangesAsync();
        return (draft.Id, null);
    }

    /// <summary>Nối hai cảnh báo thành một dòng; bỏ qua vế rỗng.</summary>
    private static string? Join(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return first + " " + second;
    }
}
