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

    /// <summary>Ngày nhận phòng (phần ngày; giờ chuẩn lấy từ cấu hình — BR-01).</summary>
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    /// <summary>Số đêm — BR-02. Tính khi lưu, lưu lại để báo cáo khỏi tính lại.</summary>
    public int Nights { get; set; }

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
