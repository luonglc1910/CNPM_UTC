using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Hóa đơn chốt từ folio khi check-out — FR-F05.
/// Không sửa sau khi chốt; sai thì hủy (Void) và lập lại (FR-F06).
/// </summary>
public class Invoice : BaseEntity
{
    /// <summary>Số hóa đơn dạng HD-yyyyMM-##### — liên tục, không trùng, không cấp lại.</summary>
    [MaxLength(20)]
    public string InvoiceNo { get; set; } = string.Empty;

    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;

    public DateTime IssuedAt { get; set; }
    public int IssuedBy { get; set; }

    /// <summary>Ca làm việc chốt hóa đơn — BR-10.</summary>
    public int CashierShiftId { get; set; }
    public CashierShift CashierShift { get; set; } = null!;

    // Các con số chốt cứng tại thời điểm xuất, để in lại không bị lệch — BR-04
    public decimal RoomCharge { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DepositAmount { get; set; }

    /// <summary>Tiền phòng + dịch vụ + phụ thu − giảm giá − cọc.</summary>
    public decimal SubTotal { get; set; }

    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Tổng sau VAT, đã làm tròn theo BR-04.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Số tiền khách đã trả cho hóa đơn này (chưa gồm cọc đối trừ).</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Phần còn thiếu ghi công nợ khi khách không trả đủ lúc check-out — REQUIREMENTS mục 6.3, SCR-D08.
    /// Bằng 0 với hóa đơn thanh toán đủ. Chỉ khác 0 khi Status = Debt.
    /// </summary>
    public decimal DebtAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Settled;

    // Hủy hóa đơn — FR-F06, chỉ Admin
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }

    [MaxLength(500)]
    public string? VoidReason { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
