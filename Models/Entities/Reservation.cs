using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Đơn đặt phòng — FR-C03, FR-C04. Một đơn có thể gồm nhiều phòng.</summary>
public class Reservation : BaseEntity
{
    /// <summary>Mã đơn dạng RSV-yyMMdd-#### — duy nhất, sinh tự động.</summary>
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Khách đứng tên đơn.</summary>
    public int PrimaryGuestId { get; set; }
    public Guest PrimaryGuest { get; set; } = null!;

    /// <summary>
    /// Hình thức thuê — BR-13. Chốt ở mức đơn: mọi phòng trong một đơn dùng chung
    /// một hình thức, vì cặp giờ đến/đi là của cả đơn chứ không của từng dòng phòng.
    /// </summary>
    public RentalType RentalType { get; set; } = RentalType.Daily;

    /// <summary>
    /// Thời điểm nhận phòng — luôn có phần giờ thật, không còn là 00:00 (BR-13):
    /// theo ngày lấy giờ chuẩn từ cấu hình, theo giờ lấy đúng giờ nhập, qua đêm lấy giờ mở gói.
    /// Không có phần giờ thì một lượt thuê giờ buổi sáng sẽ bị coi là đụng đơn trả phòng cùng hôm đó.
    /// </summary>
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    /// <summary>Số đêm — BR-02. Tính khi lưu, lưu lại để báo cáo khỏi tính lại. Thuê theo giờ luôn là 0.</summary>
    public int Nights { get; set; }

    /// <summary>Số giờ dự kiến khi thuê theo giờ — BR-13. Các hình thức khác luôn là 0.</summary>
    public int Hours { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Draft;
    public ReservationSource Source { get; set; } = ReservationSource.Phone;

    /// <summary>Hạn giữ chỗ cho đơn chưa cọc — BR-05, mặc định 18:00 ngày đến.</summary>
    public DateTime? HoldUntil { get; set; }

    public decimal EstimatedTotal { get; set; }

    [MaxLength(500)]
    public string? SpecialRequests { get; set; }

    [MaxLength(500)]
    public string? InternalNotes { get; set; }

    // Hủy đơn / no-show — FR-C07, FR-C08
    public DateTime? CancelledAt { get; set; }
    public int? CancelledBy { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>Phí hủy hoặc phí no-show đã thu — BR-05.</summary>
    public decimal CancellationFee { get; set; }

    public ICollection<ReservationRoom> Rooms { get; set; } = new List<ReservationRoom>();
    public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
    public ICollection<Stay> Stays { get; set; } = new List<Stay>();
}
