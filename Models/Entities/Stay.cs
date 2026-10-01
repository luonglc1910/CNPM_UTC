using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Lượt lưu trú thực tế của một phòng — FR-D02.
/// Mỗi Stay có đúng một Folio, kể cả khi đổi phòng nhiều lần (BR-09).
///
/// ReservationId vẫn cho phép trống để đọc được dữ liệu cũ từ khi còn màn hình khách vãng lai
/// (SCR-D03, đã bỏ). Mọi lượt lưu trú tạo mới đều đi qua check-in của một đơn đặt phòng.
/// </summary>
public class Stay : BaseEntity
{
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int PrimaryGuestId { get; set; }
    public Guest PrimaryGuest { get; set; } = null!;

    public DateTime ActualCheckIn { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
    public DateTime? ActualCheckOut { get; set; }

    /// <summary>Hình thức thuê, sao từ đơn lúc check-in — BR-13.</summary>
    public RentalType RentalType { get; set; } = RentalType.Daily;

    /// <summary>Giá/đêm chốt tại thời điểm check-in, không đổi khi sửa bảng giá — BR-02.</summary>
    public decimal PricePerNight { get; set; }

    // Ảnh giá theo giờ và qua đêm, cũng chốt lúc check-in — BR-02, BR-13.
    public decimal PriceFirstHour { get; set; }
    public decimal PriceExtraHour { get; set; }
    public decimal PriceOvernight { get; set; }

    /// <summary>
    /// Số đêm thực tế, chốt khi check-out; trước đó là số đêm dự kiến.
    /// Thuê theo giờ luôn là 0 — số giờ nằm ở <see cref="BilledHours"/>.
    /// </summary>
    public int Nights { get; set; }

    /// <summary>
    /// Số giờ đã tính tiền — BR-13. Thuê theo giờ: số giờ dự kiến lúc nhận, số giờ thật lúc trả.
    /// Qua đêm: số giờ quá gói. Theo ngày: luôn 0.
    /// </summary>
    public int BilledHours { get; set; }

    public StayStatus Status { get; set; } = StayStatus.CheckedIn;

    /// <summary>
    /// Lễ tân đã xác nhận kiểm phòng và minibar — điều kiện bắt buộc để check-out (BR-08).
    /// InspectedBy là người bấm xác nhận, không nhất thiết là người trực tiếp đi kiểm.
    /// </summary>
    public bool IsInspected { get; set; }
    public DateTime? InspectedAt { get; set; }
    public int? InspectedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Folio? Folio { get; set; }
    public ICollection<StayGuest> Guests { get; set; } = new List<StayGuest>();
    public ICollection<RoomChangeLog> RoomChanges { get; set; } = new List<RoomChangeLog>();
    public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
}
