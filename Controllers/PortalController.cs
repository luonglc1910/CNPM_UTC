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
            .FirstOrDefaultAsync(rt => rt.Id == id && rt.IsActive);
            
        if (roomType == null)
            return NotFound();
            
        ViewBag.Promotions = await _context.Promotions
            .Where(p => p.IsActive)
            .Select(p => new { p.PromoCode, p.DiscountDailyPercent, p.DiscountHourlyPercent, p.DiscountOvernightPercent, p.StartDate, p.EndDate, p.ApplicableRoomTypeIds })
            .ToListAsync();
            
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
}
