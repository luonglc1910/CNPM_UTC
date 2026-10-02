using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Web.Models.Entities;

public enum PointTransactionType
{
    Earn = 1,
    Redeem = 2,
    Expire = 3,
    Adjust = 4
}

/// <summary>Sổ cái lưu trữ lịch sử điểm thưởng của khách hàng (Ledger)</summary>
public class PointTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int GuestId { get; set; }
    public Guest? Guest { get; set; }

    /// <summary>Booking liên quan đến giao dịch tích/tiêu điểm (nếu có)</summary>
    public int? BookingId { get; set; }
    public Reservation? Booking { get; set; }

    public PointTransactionType Type { get; set; }

    /// <summary>Số điểm cộng (+) hoặc trừ (-)</summary>
    public int Amount { get; set; }

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
