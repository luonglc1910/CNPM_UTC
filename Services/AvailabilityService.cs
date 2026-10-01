using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>Một phòng còn trống trong khoảng ngày đã tra — kết quả của <see cref="IAvailabilityService"/>.</summary>
public class AvailableRoom
{
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public int Floor { get; init; }

    public int RoomTypeId { get; init; }
    public string RoomTypeCode { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;

    public decimal PricePerNight { get; init; }
    public decimal PriceFirstHour { get; init; }
    public decimal PriceExtraHour { get; init; }
    public decimal PriceOvernight { get; init; }
    public int StandardCapacity { get; init; }
    public int MaxCapacity { get; init; }

    /// <summary>Phòng đang chờ dọn — bán được cho ngày mai, nhưng nhận hôm nay thì phải dọn trước (SCR-C02).</summary>
    public bool NeedsCleaning { get; init; }
}

/// <summary>Một xung đột lịch khiến phòng không đặt được — dùng dựng thông báo lỗi rõ ràng (BR-06).</summary>
public class RoomConflict
{
    /// <summary>"Reservation" hoặc "Stay".</summary>
    public string Kind { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}

/// <summary>
/// Kiểm tra phòng trống và chống trùng lịch — BR-06.
///
/// Điều kiện chồng lấn: <c>existing.CheckIn &lt; req.CheckOut AND existing.CheckOut &gt; req.CheckIn</c>
/// (ngày trả trùng ngày nhận thì KHÔNG tính chồng lấn). Xét cả đơn đặt ở trạng thái Confirmed/CheckedIn
/// lẫn lượt lưu trú đang mở; loại phòng Maintenance/OutOfService/đã ngừng khai thác.
///
/// Mọi phép lọc đẩy xuống DB bằng NOT EXISTS, không nạp toàn bộ phòng về RAM để lọc.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>Danh sách phòng còn trống trong [checkIn, checkOut); lọc theo loại phòng và sức chứa nếu có.</summary>
    Task<IReadOnlyList<AvailableRoom>> GetAvailableRoomsAsync(
        DateTime checkIn, DateTime checkOut, int? roomTypeId = null, int? minCapacity = null);

    /// <summary>Một phòng cụ thể có trống trong khoảng ngày không — dùng khi lưu đơn, đổi phòng, gia hạn.</summary>
    Task<bool> IsRoomAvailableAsync(
        int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeReservationId = null, int? excludeStayId = null);

    /// <summary>Liệt kê các xung đột của một phòng để báo rõ trùng với đơn/lượt nào (SCR-C04).</summary>
    Task<IReadOnlyList<RoomConflict>> FindConflictsAsync(
        int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeReservationId = null, int? excludeStayId = null);
}

/// <inheritdoc />
public class AvailabilityService : IAvailabilityService
{
    private readonly HotelDbContext _db;

    public AvailabilityService(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AvailableRoom>> GetAvailableRoomsAsync(
        DateTime checkIn, DateTime checkOut, int? roomTypeId = null, int? minCapacity = null)
    {
        var query = _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive
                && r.Status != RoomStatus.Maintenance
                && r.Status != RoomStatus.OutOfService);

        if (roomTypeId is not null)
        {
            query = query.Where(r => r.RoomTypeId == roomTypeId);
        }

        if (minCapacity is not null)
        {
            query = query.Where(r => r.RoomType.MaxCapacity >= minCapacity);
        }

        query = query
            .Where(r => !_db.ReservationRooms.Any(rr =>
                rr.RoomId == r.Id
                && (rr.Reservation.Status == ReservationStatus.Confirmed
                    || rr.Reservation.Status == ReservationStatus.CheckedIn)
                && rr.Reservation.CheckInDate < checkOut
                && rr.Reservation.CheckOutDate > checkIn))
            // Lượt thuê theo giờ đang mở chưa có giờ đi (BR-13): ExpectedCheckOut chỉ là mốc tạm
            // một giờ. Nếu vẫn so theo mốc đó thì qua một giờ phòng lại hiện ra là trống trong khi
            // khách còn nằm trong đó. Chừng nào chưa trả phòng thì nó chặn mọi khoảng phía sau.
            .Where(r => !_db.Stays.Any(s =>
                s.RoomId == r.Id
                && s.Status == StayStatus.CheckedIn
                && s.ActualCheckIn < checkOut
                && (s.RentalType == RentalType.Hourly || s.ExpectedCheckOut > checkIn)));

        return await query
            .OrderBy(r => r.RoomType.Code).ThenBy(r => r.RoomNumber)
            .Select(r => new AvailableRoom
            {
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeId = r.RoomTypeId,
                RoomTypeCode = r.RoomType.Code,
                RoomTypeName = r.RoomType.Name,
                PricePerNight = r.RoomType.BasePricePerNight,
                PriceFirstHour = r.RoomType.PriceFirstHour,
                PriceExtraHour = r.RoomType.PriceExtraHour,
                PriceOvernight = r.RoomType.PriceOvernight,
                StandardCapacity = r.RoomType.StandardCapacity,
                MaxCapacity = r.RoomType.MaxCapacity,
                NeedsCleaning = r.Status == RoomStatus.Dirty
            })
            .ToListAsync();
    }

    public async Task<bool> IsRoomAvailableAsync(
        int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeReservationId = null, int? excludeStayId = null)
    {
        var usable = await _db.Rooms.AsNoTracking().AnyAsync(r =>
            r.Id == roomId
            && r.IsActive
            && r.Status != RoomStatus.Maintenance
            && r.Status != RoomStatus.OutOfService);

        if (!usable)
        {
            return false;
        }

        var conflicts = await FindConflictsAsync(roomId, checkIn, checkOut, excludeReservationId, excludeStayId);
        return conflicts.Count == 0;
    }

    public async Task<IReadOnlyList<RoomConflict>> FindConflictsAsync(
        int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeReservationId = null, int? excludeStayId = null)
    {
        var reservationConflicts = await _db.ReservationRooms.AsNoTracking()
            .Where(rr => rr.RoomId == roomId
                && (rr.Reservation.Status == ReservationStatus.Confirmed
                    || rr.Reservation.Status == ReservationStatus.CheckedIn)
                && rr.Reservation.CheckInDate < checkOut
                && rr.Reservation.CheckOutDate > checkIn
                && (excludeReservationId == null || rr.ReservationId != excludeReservationId))
            .Select(rr => new RoomConflict
            {
                Kind = "Reservation",
                Reference = rr.Reservation.Code,
                From = rr.Reservation.CheckInDate,
                To = rr.Reservation.CheckOutDate
            })
            .ToListAsync();

        var stayConflicts = await _db.Stays.AsNoTracking()
            .Where(s => s.RoomId == roomId
                && s.Status == StayStatus.CheckedIn
                && s.ActualCheckIn < checkOut
                // Thuê theo giờ chưa trả phòng thì chưa biết bao giờ trả — chặn tới khi trả (BR-13).
                && (s.RentalType == RentalType.Hourly || s.ExpectedCheckOut > checkIn)
                && (excludeStayId == null || s.Id != excludeStayId))
            .Select(s => new RoomConflict
            {
                Kind = "Stay",
                Reference = s.Room.RoomNumber,
                From = s.ActualCheckIn,
                To = s.ExpectedCheckOut
            })
            .ToListAsync();

        return reservationConflicts.Concat(stayConflicts).ToList();
    }
}
