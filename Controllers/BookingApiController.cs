using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

/// <summary>
/// API công khai cho Web Client (Next.js) — không yêu cầu đăng nhập Admin.
/// Tất cả endpoints đều có prefix /api/v1/booking.
/// CORS được cho phép từ localhost:3000 (Next.js dev server).
/// </summary>
[ApiController]
[Route("api/v1/booking")]
[AllowAnonymous]
public class BookingApiController : ControllerBase
{
    private readonly HotelDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly IPricingService _pricing;
    private readonly INumberSequenceService _numbers;
    private readonly ITransactionRunner _tx;
    private readonly IGuestService _guestService;

    public BookingApiController(
        HotelDbContext db,
        IAvailabilityService availability,
        IPricingService pricing,
        INumberSequenceService numbers,
        ITransactionRunner tx,
        IGuestService guestService)
    {
        _db = db;
        _availability = availability;
        _pricing = pricing;
        _numbers = numbers;
        _tx = tx;
        _guestService = guestService;
    }

    // ────────────────────────────────────────────────────────────
    // GET /api/v1/booking/room-types
    // Trả về danh sách loại phòng đang hoạt động + amenities + giá
    // ────────────────────────────────────────────────────────────
    [HttpGet("room-types")]
    public async Task<IActionResult> GetRoomTypes()
    {
        var roomTypes = await _db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.Code)
            .Select(rt => new RoomTypeDto
            {
                Id = rt.Id,
                Code = rt.Code,
                Name = rt.Name,
                StandardCapacity = rt.StandardCapacity,
                MaxCapacity = rt.MaxCapacity,
                BasePricePerNight = rt.BasePricePerNight,
                ExtraGuestFeePerNight = rt.ExtraGuestFeePerNight,
                ExtraBedFeePerNight = rt.ExtraBedFeePerNight,
                PriceFirstHour = rt.PriceFirstHour,
                PriceExtraHour = rt.PriceExtraHour,
                PriceOvernight = rt.PriceOvernight,
                Amenities = rt.Amenities,
                Description = rt.Description,
                TotalRooms = rt.Rooms.Count(r => r.IsActive)
            })
            .ToListAsync();

        return Ok(roomTypes);
    }

    // ────────────────────────────────────────────────────────────
    // GET /api/v1/booking/availability?checkIn=...&checkOut=...&guests=2
    // Trả về phòng trống theo loại phòng, đã group, cho khoảng ngày cụ thể
    // ────────────────────────────────────────────────────────────
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] int? guests = null)
    {
        if (checkOut.Date <= checkIn.Date)
        {
            return BadRequest(new { error = "Ngày trả phòng phải sau ngày nhận phòng." });
        }

        if (checkIn.Date < DateTime.Now.Date)
        {
            return BadRequest(new { error = "Ngày nhận phòng không được là ngày trong quá khứ." });
        }

        var available = await _availability.GetAvailableRoomsAsync(
            checkIn.Date, checkOut.Date, minCapacity: guests);

        var nights = _pricing.CountNights(checkIn.Date, checkOut.Date);

        var groups = available
            .GroupBy(r => r.RoomTypeId)
            .Select(g =>
            {
                var first = g.First();
                return new AvailabilityResultDto
                {
                    RoomTypeId = first.RoomTypeId,
                    RoomTypeCode = first.RoomTypeCode,
                    RoomTypeName = first.RoomTypeName,
                    PricePerNight = first.PricePerNight,
                    PriceFirstHour = first.PriceFirstHour,
                    PriceExtraHour = first.PriceExtraHour,
                    PriceOvernight = first.PriceOvernight,
                    StandardCapacity = first.StandardCapacity,
                    MaxCapacity = first.MaxCapacity,
                    AvailableCount = g.Count(),
                    Nights = nights,
                    TotalPriceEstimate = first.PricePerNight * nights,
                    Rooms = g.Select(r => new AvailableRoomDto
                    {
                        RoomId = r.RoomId,
                        RoomNumber = r.RoomNumber,
                        Floor = r.Floor,
                        NeedsCleaning = r.NeedsCleaning
                    }).ToList()
                };
            })
            .OrderBy(g => g.PricePerNight)
            .ToList();

        return Ok(new AvailabilityResponseDto
        {
            CheckIn = checkIn.Date,
            CheckOut = checkOut.Date,
            Nights = nights,
            Groups = groups
        });
    }

    // ────────────────────────────────────────────────────────────
    // POST /api/v1/booking/reservations
    // Tạo đơn đặt phòng từ client (Source = Online)
    // ────────────────────────────────────────────────────────────
    [HttpPost("reservations")]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate ngày
        if (req.CheckOut.Date <= req.CheckIn.Date)
            return BadRequest(new { error = "Ngày trả phòng phải sau ngày nhận phòng." });

        if (req.CheckIn.Date < DateTime.Now.Date)
            return BadRequest(new { error = "Ngày nhận phòng không được là ngày trong quá khứ." });

        // 1. Tìm hoặc tạo khách
        var guest = await FindOrCreateGuestAsync(req.Guest);

        // 2. Kiểm tra phòng còn trống
        var available = await _availability.GetAvailableRoomsAsync(
            req.CheckIn.Date, req.CheckOut.Date, req.RoomTypeId, req.Adults);

        if (!available.Any())
        {
            return Conflict(new { error = "Không còn phòng trống cho loại phòng này trong khoảng thời gian đã chọn." });
        }

        // Lấy phòng đầu tiên còn trống của loại phòng
        var room = available
            .Where(r => r.RoomTypeId == req.RoomTypeId)
            .OrderBy(r => r.NeedsCleaning) // ưu tiên phòng đã sạch
            .FirstOrDefault();

        if (room == null)
        {
            return Conflict(new { error = "Không có phòng phù hợp cho loại phòng đã chọn." });
        }

        // 3. Tính giá
        var nights = _pricing.CountNights(req.CheckIn.Date, req.CheckOut.Date);
        var basePrice = room.PricePerNight * nights;
        var extraGuests = Math.Max(0, req.Adults - room.StandardCapacity);
        var roomTypeEntity = await _db.RoomTypes.FindAsync(req.RoomTypeId);
        var extraFee = extraGuests * (roomTypeEntity?.ExtraGuestFeePerNight ?? 0m) * nights;
        var estimatedTotal = basePrice + extraFee;

        // 4. Tạo đơn
        try
        {
            var reservation = new Reservation
            {
                PrimaryGuestId = guest.Id,
                RentalType = RentalType.Daily,
                CheckInDate = req.CheckIn.Date.AddHours(14), // giờ nhận phòng chuẩn 14:00
                CheckOutDate = req.CheckOut.Date.AddHours(12), // giờ trả phòng chuẩn 12:00
                Nights = nights,
                Hours = 0,
                Status = ReservationStatus.Draft,
                Source = ReservationSource.Other, // Online booking
                SpecialRequests = req.SpecialRequests?.Trim(),
                EstimatedTotal = estimatedTotal,
                Rooms = new List<ReservationRoom>
                {
                    new ReservationRoom
                    {
                        RoomId = room.RoomId,
                        RoomTypeId = room.RoomTypeId,
                        Adults = req.Adults,
                        Children = req.Children,
                        PricePerNight = room.PricePerNight,
                        PriceFirstHour = room.PriceFirstHour,
                        PriceExtraHour = room.PriceExtraHour,
                        PriceOvernight = room.PriceOvernight
                    }
                }
            };

            var newId = await _tx.ExecuteAsync(async () =>
            {
                reservation.Code = await _numbers.NextReservationCodeAsync(DateTime.Now);
                _db.Reservations.Add(reservation);
                await _db.SaveChangesAsync();
                return reservation.Id;
            });

            return Ok(new CreateReservationResponse
            {
                ReservationId = newId,
                ReservationCode = reservation.Code,
                GuestName = guest.FullName,
                CheckIn = reservation.CheckInDate,
                CheckOut = reservation.CheckOutDate,
                RoomNumber = room.RoomNumber,
                RoomTypeName = room.RoomTypeName,
                Nights = nights,
                EstimatedTotal = estimatedTotal,
                Status = "Draft",
                Message = $"Đặt phòng thành công! Mã đặt phòng của bạn là {reservation.Code}. Khách sạn sẽ liên hệ để xác nhận."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi tạo đơn đặt phòng. Vui lòng thử lại.", detail = ex.Message });
        }
    }

    // ────────────────────────────────────────────────────────────
    // GET /api/v1/booking/reservations/{code}
    // Tra cứu đơn đặt phòng bằng mã và số điện thoại
    // ────────────────────────────────────────────────────────────
    [HttpGet("reservations/{code}")]
    public async Task<IActionResult> GetReservation(string code, [FromQuery] string phone)
    {
        var reservation = await _db.Reservations
            .AsNoTracking()
            .Include(r => r.PrimaryGuest)
            .Include(r => r.Rooms).ThenInclude(rr => rr.Room)
            .Include(r => r.Rooms).ThenInclude(rr => rr.RoomType)
            .Where(r => r.Code == code && r.PrimaryGuest.PhoneNumber == phone)
            .FirstOrDefaultAsync();

        if (reservation == null)
        {
            return NotFound(new { error = "Không tìm thấy đơn đặt phòng. Vui lòng kiểm tra lại mã và số điện thoại." });
        }

        return Ok(new ReservationLookupDto
        {
            ReservationId = reservation.Id,
            Code = reservation.Code,
            Status = reservation.Status.ToString(),
            GuestName = reservation.PrimaryGuest.FullName,
            PhoneNumber = reservation.PrimaryGuest.PhoneNumber,
            CheckIn = reservation.CheckInDate,
            CheckOut = reservation.CheckOutDate,
            Nights = reservation.Nights,
            EstimatedTotal = reservation.EstimatedTotal,
            SpecialRequests = reservation.SpecialRequests,
            Rooms = reservation.Rooms.Select(rr => new ReservationRoomDto
            {
                RoomNumber = rr.Room?.RoomNumber ?? "N/A",
                RoomTypeName = rr.RoomType?.Name ?? "N/A",
                Adults = rr.Adults,
                Children = rr.Children,
                PricePerNight = rr.PricePerNight,
            }).ToList()
        });
    }

    // ────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────
    // ────────────────────────────────────────────────────────────
    // GET /api/v1/booking/services
    // Danh mục dịch vụ khách sạn (F&B, Spa, Giặt ủi, ...) từ Admin
    // ────────────────────────────────────────────────────────────
    [HttpGet("services")]
    public async Task<IActionResult> GetServices([FromQuery] string? category = null)
    {
        var query = _db.HotelServices.AsNoTracking().Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ServiceCategory>(category, true, out var cat))
        {
            query = query.Where(s => s.Category == cat);
        }

        var rawList = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id, s.Code, s.Name, s.Category, s.UnitPrice, s.Unit,
                s.IsStockManaged, s.StockQuantity
            })
            .ToListAsync();

        var services = rawList.Select(s => new HotelServiceDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            Category = s.Category.ToString(),
            CategoryLabel = s.Category switch
            {
                ServiceCategory.FoodAndBeverage => "Ăn uống",
                ServiceCategory.Minibar => "Minibar",
                ServiceCategory.Laundry => "Giặt ủi",
                ServiceCategory.Transport => "Vận chuyển",
                _ => "Dịch vụ khác"
            },
            UnitPrice = s.UnitPrice,
            Unit = s.Unit,
            IsStockManaged = s.IsStockManaged,
            InStock = !s.IsStockManaged || s.StockQuantity > 0,
            StockQuantity = s.IsStockManaged ? s.StockQuantity : null,
        }).ToList();

        return Ok(services);
    }

    // ────────────────────────────────────────────────────────────
    // POST /api/v1/booking/guests/lookup
    // Tra cứu thông tin khách bằng số điện thoại (để tự điền form)
    // ────────────────────────────────────────────────────────────
    [HttpPost("guests/lookup")]
    public async Task<IActionResult> LookupGuest([FromBody] GuestLookupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PhoneNumber))
            return BadRequest(new { error = "Số điện thoại không được để trống." });

        var guest = await _db.Guests.AsNoTracking()
            .Where(g => g.PhoneNumber == req.PhoneNumber && !g.IsBlacklisted)
            .Select(g => new { g.FullName, g.Email, g.PhoneNumber })
            .FirstOrDefaultAsync();

        if (guest == null)
            return NotFound(new { found = false });

        return Ok(new { found = true, fullName = guest.FullName, email = guest.Email, phoneNumber = guest.PhoneNumber });
    }

    // ────────────────────────────────────────────────────────────
    private async Task<Guest> FindOrCreateGuestAsync(GuestInfoRequest info)
    {
        // Tìm theo số điện thoại trước
        var existing = await _db.Guests
            .Where(g => g.PhoneNumber == info.PhoneNumber)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            return existing;
        }

        // Tạo mới khách nếu chưa tồn tại
        var guest = new Guest
        {
            FullName = info.FullName.Trim(),
            PhoneNumber = info.PhoneNumber.Trim(),
            Email = info.Email?.Trim(),
            IdType = GuestIdType.CitizenId,
            IdNumber = info.IdNumber?.Trim() ?? "N/A",
            Nationality = "Việt Nam"
        };

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();
        return guest;
    }
}

// ================================================================
// DTOs — Data Transfer Objects cho API response
// ================================================================

public class RoomTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public decimal ExtraGuestFeePerNight { get; set; }
    public decimal ExtraBedFeePerNight { get; set; }
    public decimal PriceFirstHour { get; set; }
    public decimal PriceExtraHour { get; set; }
    public decimal PriceOvernight { get; set; }
    public string? Amenities { get; set; }
    public string? Description { get; set; }
    public int TotalRooms { get; set; }
}

public class AvailabilityResponseDto
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Nights { get; set; }
    public List<AvailabilityResultDto> Groups { get; set; } = new();
}

public class AvailabilityResultDto
{
    public int RoomTypeId { get; set; }
    public string RoomTypeCode { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal PriceFirstHour { get; set; }
    public decimal PriceExtraHour { get; set; }
    public decimal PriceOvernight { get; set; }
    public int StandardCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public int AvailableCount { get; set; }
    public int Nights { get; set; }
    public decimal TotalPriceEstimate { get; set; }
    public List<AvailableRoomDto> Rooms { get; set; } = new();
}

public class AvailableRoomDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public bool NeedsCleaning { get; set; }
}

public class CreateReservationRequest
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int RoomTypeId { get; set; }
    public int Adults { get; set; } = 1;
    public int Children { get; set; } = 0;
    public string? SpecialRequests { get; set; }
    public GuestInfoRequest Guest { get; set; } = new();
}

public class GuestInfoRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? IdNumber { get; set; }
}

public class CreateReservationResponse
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int Nights { get; set; }
    public decimal EstimatedTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ReservationLookupDto
{
    public int ReservationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal EstimatedTotal { get; set; }
    public string? SpecialRequests { get; set; }
    public List<ReservationRoomDto> Rooms { get; set; } = new();
}

public class ReservationRoomDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight { get; set; }
}

public class HotelServiceDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsStockManaged { get; set; }
    public bool InStock { get; set; }
    public int? StockQuantity { get; set; }
}

public class GuestLookupRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}
