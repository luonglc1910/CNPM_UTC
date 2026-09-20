using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Ca làm việc thu ngân — FR-F08, BR-10.
/// Ca đã đóng là bất biến: không sửa, không xóa, kể cả Admin.
/// </summary>
public class CashierShift : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    /// <summary>Quỹ tiền mặt đầu ca.</summary>
    public decimal OpeningCash { get; set; }

    /// <summary>Tiền mặt đếm thực tế khi đóng ca.</summary>
    public decimal? CountedCash { get; set; }

    /// <summary>Tiền mặt sổ sách = quỹ đầu ca + thu tiền mặt − hoàn tiền mặt.</summary>
    public decimal? ExpectedCash { get; set; }

    /// <summary>CountedCash − ExpectedCash. Khác 0 thì bắt buộc có lý do.</summary>
    public decimal? CashDifference { get; set; }

    [MaxLength(500)]
    public string? DifferenceReason { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
