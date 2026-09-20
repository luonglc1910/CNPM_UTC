using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Lượt lưu trú thực tế của một phòng — FR-D02, FR-D03.
/// ReservationId để trống nghĩa là khách vãng lai (walk-in).
/// Mỗi Stay có đúng một Folio, kể cả khi đổi phòng nhiều lần (BR-09).
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

    /// <summary>Giá/đêm chốt tại thời điểm check-in, không đổi khi sửa bảng giá — BR-02.</summary>
    public decimal PricePerNight { get; set; }

    /// <summary>Số đêm thực tế, chốt khi check-out; trước đó là số đêm dự kiến.</summary>
    public int Nights { get; set; }

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
