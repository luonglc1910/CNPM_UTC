namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Tiền cọc — FR-C05, FR-F04, BR-05.
/// Cọc chưa phải doanh thu: chỉ thành doanh thu khi đối trừ vào hóa đơn (Applied)
/// hoặc chuyển thành phí hủy / no-show (Forfeited).
/// Gắn với Reservation khi khách đặt trước, gắn với Stay khi là khách vãng lai.
/// </summary>
public class Deposit : BaseEntity
{
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public int? StayId { get; set; }
    public Stay? Stay { get; set; }

    public decimal Amount { get; set; }

    public DepositStatus Status { get; set; } = DepositStatus.Held;

    public DateTime ReceivedAt { get; set; }

    /// <summary>Số tiền đã hoàn lại khách khi cọc thừa hoặc hủy đơn được hoàn.</summary>
    public decimal RefundedAmount { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
