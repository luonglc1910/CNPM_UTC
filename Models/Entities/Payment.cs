using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Giao dịch thu/chi tiền — FR-F03, FR-F04.
/// Mọi giao dịch đều phải thuộc một ca đang mở (BR-10).
/// Khoản hoàn tiền và điều chỉnh do hủy hóa đơn mang số tiền âm.
/// </summary>
public class Payment : BaseEntity
{
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Âm với Refund / VoidAdjustment.</summary>
    public decimal Amount { get; set; }

    /// <summary>Mã giao dịch ngân hàng hoặc mã chuẩn chi của máy POS — bắt buộc với CK/Thẻ.</summary>
    [MaxLength(50)]
    public string? TransactionRef { get; set; }

    public DateTime PaidAt { get; set; }

    public int CashierShiftId { get; set; }
    public CashierShift CashierShift { get; set; } = null!;

    /// <summary>Có giá trị khi thu tiền hóa đơn hoặc điều chỉnh do hủy hóa đơn.</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    /// <summary>Có giá trị khi thu hoặc hoàn tiền cọc.</summary>
    public int? DepositId { get; set; }
    public Deposit? Deposit { get; set; }

    /// <summary>Có giá trị khi thu phí hủy hoặc phí no-show — BR-05.</summary>
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
