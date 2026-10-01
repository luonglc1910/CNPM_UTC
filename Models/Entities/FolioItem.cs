using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Một dòng chi phí trên folio — FR-E01, FR-F01.
/// Dòng ghi nhầm được đánh dấu hủy (IsVoided) chứ không xóa khỏi bảng (FR-E03).
/// </summary>
public class FolioItem : BaseEntity
{
    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;

    public FolioItemType ItemType { get; set; }

    /// <summary>Chỉ dùng khi ItemType = Surcharge — BR-03.</summary>
    public SurchargeType SurchargeType { get; set; } = SurchargeType.None;

    /// <summary>Chỉ dùng khi ItemType = Service.</summary>
    public int? HotelServiceId { get; set; }
    public HotelService? HotelService { get; set; }

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    /// <summary>Đơn giá chép tại thời điểm ghi nhận; sửa bảng giá sau đó không ảnh hưởng.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quantity * UnitPrice. Dòng giảm giá mang giá trị âm.</summary>
    public decimal Amount { get; set; }

    /// <summary>Ngày phát sinh chi phí (đêm nào, bán lúc nào).</summary>
    public DateTime ChargedAt { get; set; }

    // Hủy dòng chi phí — FR-E03
    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }

    [MaxLength(500)]
    public string? VoidReason { get; set; }

    /// <summary>Lý do giảm giá — bắt buộc khi ItemType = Discount (FR-F02).</summary>
    [MaxLength(500)]
    public string? DiscountReason { get; set; }
}
