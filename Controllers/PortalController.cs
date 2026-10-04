using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Web.Data;

namespace HotelManagement.Web.Controllers;

[AllowAnonymous]
public class PortalController : Controller
{
    private readonly HotelDbContext _context;

    public PortalController(HotelDbContext context)
    {
        _context = context;
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

        var overlappingReservations = await _context.ReservationRooms
            .Include(rr => rr.Reservation)
            .Where(rr => rr.Reservation.Status == HotelManagement.Web.Models.ReservationStatus.Confirmed || 
                         rr.Reservation.Status == HotelManagement.Web.Models.ReservationStatus.CheckedIn)
            .Where(rr => rr.Reservation.CheckInDate < end && rr.Reservation.CheckOutDate > start)
            .ToListAsync();

        foreach(var rt in roomTypes)
        {
            int totalRooms = rt.Rooms.Count(r => r.IsActive && r.Status != HotelManagement.Web.Models.RoomStatus.OutOfService);
            int bookedRooms = overlappingReservations.Count(rr => rr.RoomTypeId == rt.Id);
            int available = totalRooms - bookedRooms;

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

        ViewBag.OccupiedRoomIds = roomType.Rooms.Where(r => 
            r.Stays.Any(s => s.Status == HotelManagement.Web.Models.StayStatus.CheckedIn) ||
            r.ReservationRooms.Any(rr => 
                (rr.Reservation.Status == HotelManagement.Web.Models.ReservationStatus.Confirmed || 
                 rr.Reservation.Status == HotelManagement.Web.Models.ReservationStatus.CheckedIn ||
                 rr.Reservation.Status == HotelManagement.Web.Models.ReservationStatus.Draft) &&
                rr.Reservation.CheckInDate < end && rr.Reservation.CheckOutDate > start)
        ).Select(r => r.Id).ToList();
            
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
        
        int nightsOrHours = 1;
        decimal basePrice = 0;
        
        if (rentalType == "Daily")
        {
            nightsOrHours = checkOut > checkIn ? (int)(checkOut - checkIn).TotalDays : 1;
            if (nightsOrHours < 1) nightsOrHours = 1;
            basePrice = rt.BasePricePerNight;
        }
        else if (rentalType == "Hourly")
        {
            nightsOrHours = 1;
            basePrice = rt.PriceFirstHour;
        }
        else // Overnight
        {
            nightsOrHours = 1;
            basePrice = rt.PriceOvernight;
        }

        decimal extraGuestFee = 0;
        if (guests > rt.StandardCapacity)
        {
            extraGuestFee = (guests - rt.StandardCapacity) * rt.ExtraGuestFeePerNight;
        }

        decimal extraBedFee = extraBed ? rt.ExtraBedFeePerNight : 0;
        
        decimal totalRoomPrice = basePrice * nightsOrHours;
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

        if (User.Identity?.IsAuthenticated == true && User.Identity.AuthenticationType == Security.AppSchemes.Client)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

        // 1. Check Guest
        var guest = await _context.Guests.FirstOrDefaultAsync(g => g.PhoneNumber == model.PhoneNumber || (g.Email != null && g.Email == model.Email));
        if (guest == null)
        {
            guest = new HotelManagement.Web.Models.Entities.Guest
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Nationality = model.Nationality,
                IdType = HotelManagement.Web.Models.GuestIdType.CitizenId,
                IdNumber = $"W-{DateTime.Now.Ticks}" // Tạm thời dùng Ticks để tránh trùng lặp
            };
            _context.Guests.Add(guest);
            await _context.SaveChangesAsync();
        }

        // 2. Create Reservation
        var rt = room.RoomType;
        
        var res = new HotelManagement.Web.Models.Entities.Reservation
        {
            Code = "RSV-" + DateTime.Now.ToString("yyMMdd") + "-" + new Random().Next(1000, 9999),
            PrimaryGuestId = guest.Id,
            RentalType = model.RentalType == "Daily" ? HotelManagement.Web.Models.RentalType.Daily : 
                        model.RentalType == "Hourly" ? HotelManagement.Web.Models.RentalType.Hourly : 
                        HotelManagement.Web.Models.RentalType.Overnight,
            CheckInDate = model.CheckIn,
            CheckOutDate = model.CheckOut,
            Status = HotelManagement.Web.Models.ReservationStatus.Confirmed,
            Source = HotelManagement.Web.Models.ReservationSource.Other,
            SpecialRequests = model.ArrivalTime
        };

        if (res.RentalType == HotelManagement.Web.Models.RentalType.Daily)
        {
            res.Nights = model.CheckOut > model.CheckIn ? (int)(model.CheckOut - model.CheckIn).TotalDays : 1;
            if (res.Nights < 1) res.Nights = 1;
        }

        int nightsOrHours = 1;
        decimal basePrice = 0;
        
        if (model.RentalType == "Daily")
        {
            nightsOrHours = res.Nights > 0 ? res.Nights : 1;
            basePrice = rt.BasePricePerNight;
        }
        else if (model.RentalType == "Hourly")
        {
            basePrice = rt.PriceFirstHour;
        }
        else // Overnight
        {
            basePrice = rt.PriceOvernight;
        }

        decimal extraGuestFee = 0;
        if (model.Guests > rt.StandardCapacity)
            extraGuestFee = (model.Guests - rt.StandardCapacity) * rt.ExtraGuestFeePerNight;

        decimal extraBedFee = model.ExtraBed ? rt.ExtraBedFeePerNight : 0;
        
        decimal totalRoomPrice = basePrice * nightsOrHours;
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
        var resRoom = new HotelManagement.Web.Models.Entities.ReservationRoom
        {
            ReservationId = res.Id,
            RoomTypeId = rt.Id,
            RoomId = room.Id,
            Adults = model.Guests,
            PricePerNight = rt.BasePricePerNight,
            PriceFirstHour = rt.PriceFirstHour,
            PriceOvernight = rt.PriceOvernight,
            PriceExtraHour = rt.PriceExtraHour
        };

        _context.ReservationRooms.Add(resRoom);
        
        await _context.SaveChangesAsync();

        if (model.PaymentMethod == "qr")
        {
            // Here you'd redirect to VNPAY sandbox logic
            // return Redirect("/Payment/VNPay?resId=" + res.Id);
            return RedirectToAction("BookingSuccess", new { id = res.Id });
        }

        return RedirectToAction("BookingSuccess", new { id = res.Id });
    }

    public async Task<IActionResult> BookingSuccess(int id)
    {
        var res = await _context.Reservations
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Rooms)
                .ThenInclude(rr => rr.RoomType)
            .Include(r => r.Rooms)
                .ThenInclude(rr => rr.Room)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (res == null) return NotFound();
        
        return View(res);
    }
}
