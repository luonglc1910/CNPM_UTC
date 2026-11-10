using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Services;

namespace HotelManagement.Web.Controllers;

[AllowAnonymous]
public class PortalController : Controller
{
    private readonly HotelDbContext _context;
    private readonly IPricingService _pricing;
    private readonly ISettingsReader _settings;
    private readonly INumberSequenceService _numbers;
    private readonly IAvailabilityService _availability;
    private readonly IVietQrService _vietQr;
    private readonly IVnPayService _vnPay;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly ILogger<PortalController> _logger;

    public PortalController(
        HotelDbContext context,
        IPricingService pricing,
        ISettingsReader settings,
        INumberSequenceService numbers,
        IAvailabilityService availability,
        IVietQrService vietQr,
        IVnPayService vnPay,
        INotificationService notifications,
        IEmailService email,
        IAuditService audit,
        ILogger<PortalController> logger)
    {
        _context = context;
        _pricing = pricing;
        _settings = settings;
        _numbers = numbers;
        _availability = availability;
        _vietQr = vietQr;
        _vnPay = vnPay;
        _notifications = notifications;
        _email = email;
        _audit = audit;
        _logger = logger;
    }
    public async Task<IActionResult> Index()
    {
        ViewBag.RoomTypes = await _context.RoomTypes.Where(rt => rt.IsActive).ToListAsync();
        ViewBag.Services = await _context.HotelServices.Where(s => s.IsActive).Take(6).ToListAsync();
        ViewBag.GalleryImages = await _context.HotelGalleryImages
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder)
            .ThenByDescending(g => g.Id)
            .ToListAsync();
        return View();
    }

    public async Task<IActionResult> Search(DateTime? checkIn, DateTime? checkOut, int? capacity, int? categoryId)
    {
        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        ViewBag.Capacity = capacity;
        ViewBag.CategoryId = categoryId;

        var query = _context.RoomTypes
            .Include(rt => rt.Rooms)
            .Where(rt => rt.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(rt => rt.Id == categoryId.Value);
        }

        if (capacity.HasValue)
        {
            query = query.Where(rt => rt.MaxCapacity >= capacity.Value);
        }

        var roomTypes = await query.ToListAsync();

        var availableCounts = new Dictionary<int, int>();
        var validRoomTypes = new List<HotelManagement.Web.Models.Entities.RoomType>();

        DateTime start = checkIn ?? DateTime.Today;
        DateTime end = checkOut ?? DateTime.Today.AddDays(1);

        var availableRooms = await _availability.GetAvailableRoomsAsync(start, end, null, capacity);
        var availableByType = availableRooms
            .GroupBy(r => r.RoomTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach(var rt in roomTypes)
        {
            int available = availableByType.GetValueOrDefault(rt.Id);

            if (available > 0)
            {
                availableCounts[rt.Id] = available;
                validRoomTypes.Add(rt);
            }
        }

        ViewBag.AvailableCounts = availableCounts;
        ViewBag.RoomTypes = await _context.RoomTypes.Where(rt => rt.IsActive).ToListAsync();

        return View(validRoomTypes);
    }

    public async Task<IActionResult> RoomDetails(int id)
    {
        var roomType = await _context.RoomTypes
            .Include(rt => rt.Rooms)
                .ThenInclude(r => r.Stays)
            .Include(rt => rt.Rooms)
                .ThenInclude(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Reservation)
            .FirstOrDefaultAsync(rt => rt.Id == id && rt.IsActive);
            
        if (roomType == null)
            return NotFound();
            
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Today.AddDays(1);

        var availableToday = await _availability.GetAvailableRoomsAsync(start, end, id);
        var availableTodayIds = availableToday.Select(r => r.RoomId).ToHashSet();
        ViewBag.OccupiedRoomIds = roomType.Rooms
            .Where(r => r.IsActive && !availableTodayIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToList();
            
        var allActivePromos = await _context.Promotions
            .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
            .ToListAsync();

        var suggestedPromos = allActivePromos
            .Where(p => string.IsNullOrWhiteSpace(p.ApplicableRoomTypeIds) ||
                        p.ApplicableRoomTypeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Contains(id.ToString()))
            .ToList();
            
        ViewBag.Promotions = allActivePromos.Select(p => new { 
            p.Id,
            p.Title, 
            p.Description, 
            p.PromoCode, 
            p.DiscountDailyPercent, 
            p.DiscountHourlyPercent, 
            p.DiscountOvernightPercent, 
            p.StartDate, 
            p.EndDate, 
            p.ApplicableRoomTypeIds 
        }).ToList();

        ViewBag.SuggestedPromotions = suggestedPromos;
            
        return View(roomType);
    }

    public async Task<IActionResult> Offers()
    {
        var promotions = await _context.Promotions
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsHeroOffer)
            .ThenByDescending(p => p.Id)
            .ToListAsync();
            
        return View(promotions);
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> BookRoom(int roomId, string rentalType, DateTime checkIn, DateTime checkOut, int guests, bool extraBed = false, string promoCode = "")
    {
        var room = await _context.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);

        if (room == null)
            return NotFound();

        var rt = room.RoomType;
        var pricingSettings = await _settings.GetPricingSettingsAsync();
        
        int nightsOrHours = 1;
        decimal basePrice = 0;
        decimal totalRoomPrice = 0;
        IReadOnlyList<NightPriceBreakdown> breakdown = Array.Empty<NightPriceBreakdown>();
        
        if (rentalType == "Daily")
        {
            nightsOrHours = checkOut > checkIn ? (int)(checkOut - checkIn).TotalDays : 1;
            if (nightsOrHours < 1) nightsOrHours = 1;
            basePrice = rt.BasePricePerNight;
            breakdown = _pricing.GetNightlyBreakdown(checkIn, nightsOrHours, rt.BasePricePerNight, pricingSettings, rt.Id);
            totalRoomPrice = breakdown.Sum(x => x.AppliedPrice);
        }
        else if (rentalType == "Hourly")
        {
            nightsOrHours = 1;
            basePrice = rt.PriceFirstHour;
            totalRoomPrice = basePrice * nightsOrHours;
        }
        else // Overnight
        {
            nightsOrHours = 1;
            basePrice = rt.PriceOvernight;
            totalRoomPrice = basePrice * nightsOrHours;
        }

        decimal extraGuestFee = 0;
        if (guests > rt.StandardCapacity)
        {
            extraGuestFee = (guests - rt.StandardCapacity) * rt.ExtraGuestFeePerNight;
        }

        decimal extraBedFee = extraBed ? rt.ExtraBedFeePerNight : 0;
        
        decimal totalExtras = (extraGuestFee + extraBedFee) * (rentalType == "Daily" ? nightsOrHours : 1);
        
        decimal subTotal = totalRoomPrice + totalExtras;
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(promoCode))
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == promoCode && p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
            if (promo != null)
            {
                bool isApplicable = string.IsNullOrWhiteSpace(promo.ApplicableRoomTypeIds) ||
                    promo.ApplicableRoomTypeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Contains(rt.Id.ToString());

                if (isApplicable)
                {
                    if (rentalType == "Daily") discountAmount = subTotal * (promo.DiscountDailyPercent / 100m);
                    else if (rentalType == "Hourly") discountAmount = subTotal * (promo.DiscountHourlyPercent / 100m);
                    else discountAmount = subTotal * (promo.DiscountOvernightPercent / 100m);
                }
            }
        }

        string guestFullName = string.Empty;
        string guestPhone = string.Empty;
        string guestEmail = string.Empty;

        var authResult = await HttpContext.AuthenticateAsync(Security.AppSchemes.Client);
        if (authResult.Succeeded && authResult.Principal != null)
        {
            var userIdClaim = authResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int guestId))
            {
                var currentGuest = await _context.Guests.FindAsync(guestId);
                if (currentGuest != null)
                {
                    guestFullName = currentGuest.FullName ?? string.Empty;
                    guestPhone = currentGuest.PhoneNumber ?? string.Empty;
                    guestEmail = currentGuest.Email ?? string.Empty;
                }
            }
        }

        var vm = new HotelManagement.Web.Models.ViewModels.ClientBookingViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeName = rt.Name,
            ImageUrl = rt.ImageUrl,
            RentalType = rentalType,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = guests,
            ExtraBed = extraBed,
            PromoCode = promoCode,
            BasePrice = basePrice,
            NightsOrHours = nightsOrHours,
            TotalRoomPrice = totalRoomPrice,
            ExtraGuestFee = extraGuestFee * (rentalType == "Daily" ? nightsOrHours : 1),
            ExtraBedFee = extraBedFee * (rentalType == "Daily" ? nightsOrHours : 1),
            DiscountAmount = discountAmount,
            GrandTotal = subTotal - discountAmount,
            NightlyBreakdown = breakdown,
            FullName = guestFullName,
            PhoneNumber = guestPhone,
            Email = guestEmail
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> BookRoom(HotelManagement.Web.Models.ViewModels.ClientBookingPostModel model)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == model.RoomId && r.IsActive);
            
        if (room == null) return NotFound();

        var pricingSettings = await _settings.GetPricingSettingsAsync();
        var rType = model.RentalType == "Daily" ? RentalType.Daily :
                    model.RentalType == "Hourly" ? RentalType.Hourly :
                    RentalType.Overnight;

        var (period, _) = _pricing.ResolvePeriod(rType, model.CheckIn, model.CheckOut, pricingSettings);
        var actualCheckIn = period?.CheckIn ?? model.CheckIn;
        var actualCheckOut = period?.CheckOut ?? model.CheckOut;

        // Kiểm tra chống trùng lịch tại thời điểm đặt
        var isAvailable = await _availability.IsRoomAvailableAsync(room.Id, actualCheckIn, actualCheckOut);
        if (!isAvailable)
        {
            TempData["Error"] = $"Phòng {room.RoomNumber} vừa có khách khác đặt trong khoảng thời gian này. Vui lòng chọn phòng khác.";
            return RedirectToAction("RoomDetails", new { id = room.RoomTypeId });
        }

        // 1. Xác định khách đứng tên đơn.
        // Khách đã đăng nhập → luôn gắn đơn vào CHÍNH tài khoản đó, để hiện trong "Đơn đặt phòng của tôi".
        // (Trước đây khớp theo SĐT/email nên đơn bị gắn nhầm sang hồ sơ khác trùng số điện thoại.)
        // Khách vãng lai → khớp email trước (định danh chính xác hơn), rồi mới tới SĐT.
        Guest? guest = null;
        var clientAuth = await HttpContext.AuthenticateAsync(HotelManagement.Web.Security.AppSchemes.Client);
        if (clientAuth.Succeeded
            && int.TryParse(clientAuth.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var loggedInGuestId))
        {
            guest = await _context.Guests.FirstOrDefaultAsync(g => g.Id == loggedInGuestId);
        }

        if (guest == null && !string.IsNullOrWhiteSpace(model.Email))
        {
            guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email != null && g.Email == model.Email);
        }
        guest ??= await _context.Guests.FirstOrDefaultAsync(g => g.PhoneNumber == model.PhoneNumber);
        if (guest == null)
        {
            guest = new Guest
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Nationality = model.Nationality,
                IdType = GuestIdType.CitizenId,
                IdNumber = $"W-{DateTime.Now.Ticks}"
            };
            _context.Guests.Add(guest);
            await _context.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(guest.Email))
        {
            guest.Email = model.Email.Trim();
            await _context.SaveChangesAsync();
        }

        // 2. Create Reservation
        var rt = room.RoomType;
        var code = await _numbers.NextReservationCodeAsync(DateTime.Now);
        
        var res = new Reservation
        {
            Code = code,
            PrimaryGuestId = guest.Id,
            RentalType = rType,
            CheckInDate = actualCheckIn,
            CheckOutDate = actualCheckOut,
            Status = ReservationStatus.Confirmed,
            Source = ReservationSource.Other,
            SpecialRequests = model.ArrivalTime,
            InternalNotes = !string.IsNullOrWhiteSpace(model.PromoCode) ? $"Portal ({model.PaymentMethod}) - Promo: {model.PromoCode}" : $"Portal ({model.PaymentMethod})"
        };

        if (res.RentalType == RentalType.Daily)
        {
            res.Nights = period?.Nights ?? (model.CheckOut > model.CheckIn ? (int)(model.CheckOut - model.CheckIn).TotalDays : 1);
            if (res.Nights < 1) res.Nights = 1;
        }
        else if (res.RentalType == RentalType.Overnight)
        {
            res.Nights = 1;
        }
        else
        {
            res.Hours = 1;
        }

        int nightsOrHours = 1;
        decimal basePrice = 0;
        decimal totalRoomPrice = 0;
        
        if (model.RentalType == "Daily")
        {
            nightsOrHours = res.Nights > 0 ? res.Nights : 1;
            var breakdown = _pricing.GetNightlyBreakdown(actualCheckIn, nightsOrHours, rt.BasePricePerNight, pricingSettings, rt.Id);
            totalRoomPrice = breakdown.Sum(x => x.AppliedPrice);
            basePrice = nightsOrHours > 0 ? Math.Round(totalRoomPrice / nightsOrHours, 0) : rt.BasePricePerNight;
        }
        else if (model.RentalType == "Hourly")
        {
            basePrice = rt.PriceFirstHour;
            totalRoomPrice = basePrice;
        }
        else // Overnight
        {
            basePrice = rt.PriceOvernight;
            totalRoomPrice = basePrice;
        }

        decimal extraGuestFee = 0;
        if (model.Guests > rt.StandardCapacity)
            extraGuestFee = (model.Guests - rt.StandardCapacity) * rt.ExtraGuestFeePerNight;

        decimal extraBedFee = model.ExtraBed ? rt.ExtraBedFeePerNight : 0;
        decimal totalExtras = (extraGuestFee + extraBedFee) * (model.RentalType == "Daily" ? nightsOrHours : 1);
        
        decimal subTotal = totalRoomPrice + totalExtras;
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(model.PromoCode))
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == model.PromoCode && p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
            if (promo != null)
            {
                bool isApplicable = string.IsNullOrWhiteSpace(promo.ApplicableRoomTypeIds) ||
                    promo.ApplicableRoomTypeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Contains(rt.Id.ToString());

                if (isApplicable)
                {
                    if (model.RentalType == "Daily") discountAmount = subTotal * (promo.DiscountDailyPercent / 100m);
                    else if (model.RentalType == "Hourly") discountAmount = subTotal * (promo.DiscountHourlyPercent / 100m);
                    else discountAmount = subTotal * (promo.DiscountOvernightPercent / 100m);
                }
            }
        }
        
        res.EstimatedTotal = subTotal - discountAmount;

        _context.Reservations.Add(res);
        await _context.SaveChangesAsync();

        // 3. Create ReservationRoom
        var resRoom = new ReservationRoom
        {
            ReservationId = res.Id,
            RoomTypeId = rt.Id,
            RoomId = room.Id,
            Adults = model.Guests,
            PricePerNight = basePrice,
            PriceFirstHour = rt.PriceFirstHour,
            PriceOvernight = rt.PriceOvernight,
            PriceExtraHour = rt.PriceExtraHour
        };

        _context.ReservationRooms.Add(resRoom);
        _audit.Log("PortalBooking", nameof(Reservation), res.Id.ToString(),
            newValue: $"Đơn {res.Code} - Khách {guest.FullName} - Phòng {room.RoomNumber} - Tổng {res.EstimatedTotal:N0} ₫");
        await _context.SaveChangesAsync();

        // 4. Bắn thông báo SignalR thời gian thực cho Lễ tân & gửi Email xác nhận tự động
        await _notifications.PushAsync(
            "PortalBooking",
            "Đặt phòng mới từ Portal",
            $"Khách {guest.FullName} ({guest.PhoneNumber}) vừa đặt Phòng {room.RoomNumber} — Mã đơn {res.Code} ({res.EstimatedTotal:N0} ₫)",
            $"/Reservations/Details/{res.Id}",
            "calendar-check",
            "bg-primary");

        var email = await _email.SendBookingConfirmationAsync(res.Id);
        TempData["EmailSent"] = email.Sent;
        TempData["EmailMessage"] = email.Message;
        if (!string.IsNullOrEmpty(email.PreviewUrl))
        {
            TempData["EmailPreviewUrl"] = email.PreviewUrl;
        }

        if (model.PaymentMethod == "qr")
        {
            return RedirectToAction("BookingSuccess", new { id = res.Id, pay = "qr" });
        }

        return RedirectToAction("BookingSuccess", new { id = res.Id });
    }

    public async Task<IActionResult> BookingSuccess(int id, string? pay = null)
    {
        var res = await _context.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Rooms)
                .ThenInclude(rr => rr.RoomType)
            .Include(r => r.Rooms)
                .ThenInclude(rr => rr.Room)
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (res == null) return NotFound();

        var depositPaid = res.Deposits.Where(d => d.Status == DepositStatus.Held || d.Status == DepositStatus.Applied).Sum(d => d.Amount);
        var remaining = Math.Max(0, res.EstimatedTotal - depositPaid);

        ViewBag.DepositPaid = depositPaid;
        ViewBag.RemainingAmount = remaining;
        ViewBag.PayMode = pay;
        ViewBag.VnPayEnabled = _vnPay.IsConfigured;
        ViewBag.TransferReported = remaining > 0 && await _context.AuditLogs.AnyAsync(a =>
            a.Action == "PortalTransferReported" && a.EntityId == res.Id.ToString());
        ViewBag.ShowQrModal = string.Equals(pay, "qr", StringComparison.OrdinalIgnoreCase) && remaining > 0;
        ViewBag.VietQr = await _vietQr.BuildQrAsync(
            remaining > 0 ? remaining : res.EstimatedTotal,
            $"AURA {res.Code}",
            $"Thanh toán đơn đặt phòng {res.Code}");
        
        return View(res);
    }

    /// <summary>
    /// Khách báo "đã chuyển khoản VietQR". KHÔNG ghi nhận tiền — chỉ báo cho Lễ tân đối soát sao kê
    /// rồi thu cọc tại SCR-C07 (/Reservations/Deposit) với mã giao dịch ngân hàng thật (BR-10).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReportTransfer(int reservationId, string? payerName, string? payerNote)
    {
        var res = await _context.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.Id == reservationId);
        if (res == null) return NotFound();

        var paid = res.Deposits.Where(d => d.Status == DepositStatus.Held || d.Status == DepositStatus.Applied).Sum(d => d.Amount);
        var remaining = Math.Max(0, res.EstimatedTotal - paid);
        if (remaining <= 0)
        {
            return RedirectToAction("BookingSuccess", new { id = res.Id });
        }

        // Chống spam: mỗi đơn chỉ báo 1 lần trong 10 phút
        var since = DateTime.Now.AddMinutes(-10);
        var recentlyReported = await _context.AuditLogs.AnyAsync(a =>
            a.Action == "PortalTransferReported" && a.EntityId == res.Id.ToString() && a.CreatedAt >= since);

        if (!recentlyReported)
        {
            var who = string.IsNullOrWhiteSpace(payerName) ? res.PrimaryGuest.FullName : payerName.Trim();
            var note = string.IsNullOrWhiteSpace(payerNote) ? "" : $" — Ghi chú: {payerNote.Trim()}";
            if (note.Length > 300) note = note[..300];

            _audit.Log("PortalTransferReported", nameof(Reservation), res.Id.ToString(),
                newValue: $"Khách báo đã CK {remaining:N0} ₫ (nội dung: AURA {res.Code}) - Người CK: {who}{note}. CHỜ ĐỐI SOÁT.");
            await _context.SaveChangesAsync();

            try
            {
                await _notifications.PushAsync(
                    "PortalTransferReported",
                    "Khách báo đã chuyển khoản — cần đối soát",
                    $"Đơn {res.Code} ({res.PrimaryGuest.FullName}) báo đã CK {remaining:N0} ₫, nội dung \"AURA {res.Code}\". Vui lòng kiểm tra sao kê NCB rồi thu cọc.",
                    $"/Reservations/Deposit/{res.Id}",
                    "hourglass-split",
                    "bg-warning text-dark");
            }
            catch
            {
                // Bỏ qua lỗi thông báo
            }
        }

        TempData["PaymentInfo"] = "Đã gửi thông báo tới khách sạn. Đơn sẽ được xác nhận ngay khi bộ phận Lễ tân đối soát được giao dịch chuyển khoản của quý khách.";
        return RedirectToAction("BookingSuccess", new { id = res.Id });
    }

    /// <summary>Tạo URL thanh toán và chuyển khách sang cổng VNPay (thẻ nội địa NCB nhập trên trang VNPay).</summary>
    [HttpPost]
    public async Task<IActionResult> PayWithVnPay(int reservationId, string? method = "ncb")
    {
        var res = await _context.Reservations
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.Id == reservationId);
        if (res == null) return NotFound();

        if (!_vnPay.IsConfigured)
        {
            TempData["PaymentError"] = "Cổng VNPay chưa được cấu hình (thiếu VnPay:TmnCode / VnPay:HashSecret). Vui lòng thanh toán chuyển khoản VietQR.";
            return RedirectToAction("BookingSuccess", new { id = res.Id, pay = "qr" });
        }

        var paid = res.Deposits.Where(d => d.Status == DepositStatus.Held || d.Status == DepositStatus.Applied).Sum(d => d.Amount);
        var remaining = Math.Max(0, res.EstimatedTotal - paid);
        if (remaining <= 0)
        {
            return RedirectToAction("BookingSuccess", new { id = res.Id });
        }

        var returnUrl = Url.Action("VnPayReturn", "Portal", null, Request.Scheme)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        // qr  -> VNPay hiển thị mã VNPAY-QR; ncb -> vào thẳng form thẻ ATM NCB; all -> khách tự chọn trên trang VNPay
        var bankCode = method?.ToLowerInvariant() switch
        {
            "qr" => "VNPAYQR",
            "all" => null,
            _ => "NCB"
        };
        var url = _vnPay.CreatePaymentUrl(res.Id, remaining, $"Thanh toan don dat phong {res.Code}", returnUrl, ip, bankCode);
        return Redirect(url);
    }

    /// <summary>VNPay chuyển trình duyệt của khách về đây. Chỉ ghi nhận khi chữ ký hợp lệ.</summary>
    [HttpGet]
    public async Task<IActionResult> VnPayReturn()
    {
        var result = _vnPay.ParseAndVerify(Request.Query);
        var (_, message) = await ApplyVnPayResultAsync(result);

        if (result.ReservationId <= 0)
        {
            return RedirectToAction("Index");
        }

        if (result.Success)
            TempData["PaymentSuccess"] = message;
        else
            TempData["PaymentError"] = message;

        return RedirectToAction("BookingSuccess", new { id = result.ReservationId, pay = result.Success ? null : "qr" });
    }

    /// <summary>IPN: VNPay gọi server-to-server (cần URL công khai, cấu hình trên trang quản trị VNPay).</summary>
    [HttpGet]
    public async Task<IActionResult> VnPayIpn()
    {
        try
        {
            var result = _vnPay.ParseAndVerify(Request.Query);
            if (!result.SignatureValid)
                return VnPayIpnResponse("97", "Invalid signature");

            var (code, message) = await ApplyVnPayResultAsync(result);
            return VnPayIpnResponse(code, message);
        }
        catch (Exception ex)
        {
            // VNPAY dựa vào RspCode để quyết định retry IPN; không trả HTML lỗi/HTTP 500.
            _logger.LogError(ex, "Xử lý VNPay IPN thất bại");
            return VnPayIpnResponse("99", "Unknown error");
        }
    }

    private JsonResult VnPayIpnResponse(string code, string message)
        // Dictionary giữ nguyên đúng chữ hoa RspCode/Message theo contract của VNPAY.
        => Json(new Dictionary<string, string>
        {
            ["RspCode"] = code,
            ["Message"] = message
        });

    /// <summary>Đối chiếu kết quả VNPay với đơn và ghi nhận tiền cọc (idempotent theo vnp_TxnRef).</summary>
    private async Task<(string Code, string Message)> ApplyVnPayResultAsync(VnPayResult result)
    {
        if (!result.SignatureValid)
            return ("97", "Chữ ký VNPay không hợp lệ — giao dịch không được ghi nhận.");

        var res = await _context.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Deposits)
            .FirstOrDefaultAsync(r => r.Id == result.ReservationId);
        if (res == null) return ("01", "Không tìm thấy đơn đặt phòng.");

        if (!result.Success)
            return ("00", $"Thanh toán VNPay không thành công (mã {result.ResponseCode}). Quý khách vui lòng thử lại.");

        var marker = $"VNPAY:{result.TxnRef}";
        var alreadyApplied = await _context.AuditLogs.AnyAsync(a =>
            a.Action == "PortalVnPayPayment" && a.EntityId == res.Id.ToString() && a.NewValue != null && a.NewValue.Contains(marker));
        if (alreadyApplied)
            return ("02", $"Giao dịch {result.TransactionNo} đã được ghi nhận trước đó.");

        var paid = res.Deposits.Where(d => d.Status == DepositStatus.Held || d.Status == DepositStatus.Applied).Sum(d => d.Amount);
        var remaining = Math.Max(0, res.EstimatedTotal - paid);
        if (result.Amount <= 0 || result.Amount != remaining)
            return ("04", "Số tiền VNPay trả về không khớp với số tiền còn phải thanh toán của đơn.");

        var now = DateTime.Now;
        var deposit = new Deposit
        {
            ReservationId = res.Id,
            Amount = result.Amount,
            Status = DepositStatus.Held,
            ReceivedAt = now
        };
        _context.Deposits.Add(deposit);
        await _context.SaveChangesAsync();

        // Có ca thu ngân đang mở thì ghi luôn phiếu thu để khớp sổ quỹ (BR-10)
        var openShift = await _context.CashierShifts
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(s => s.Status == ShiftStatus.Open);
        if (openShift != null)
        {
            _context.Payments.Add(new Payment
            {
                Type = PaymentType.Deposit,
                Method = PaymentMethod.Card,
                Amount = result.Amount,
                TransactionRef = $"VNP{result.TransactionNo}",
                PaidAt = now,
                CashierShiftId = openShift.Id,
                DepositId = deposit.Id,
                ReservationId = res.Id,
                Notes = $"Thanh toán online VNPay ({result.BankCode}) đơn {res.Code}"
            });
        }

        if (res.Status == ReservationStatus.Draft)
        {
            res.Status = ReservationStatus.Confirmed;
        }

        _audit.Log("PortalVnPayPayment", nameof(Reservation), res.Id.ToString(),
            newValue: $"{marker} - VNPay {result.BankCode} {result.Amount:N0} ₫ cho đơn {res.Code} (Mã GD VNPay: {result.TransactionNo})");
        await _context.SaveChangesAsync();

        try
        {
            await _notifications.PushAsync(
                "PortalVnPayPayment",
                "Thanh toán VNPay thành công",
                $"Đơn {res.Code} ({res.PrimaryGuest.FullName}) đã thanh toán {result.Amount:N0} ₫ qua VNPay ({result.BankCode}) - Mã GD {result.TransactionNo}.",
                $"/Reservations/Details/{res.Id}",
                "credit-card",
                "bg-success");
            var email = await _email.SendBookingConfirmationAsync(res.Id);
            TempData["EmailSent"] = email.Sent;
            TempData["EmailMessage"] = email.Message;
            if (!string.IsNullOrEmpty(email.PreviewUrl)) TempData["EmailPreviewUrl"] = email.PreviewUrl;
        }
        catch
        {
            // Bỏ qua lỗi thông báo / email
        }

        return ("00", $"Thanh toán VNPay thành công {result.Amount:N0} ₫ (Ngân hàng {result.BankCode} · Mã GD VNPay: {result.TransactionNo}).");
    }
}
