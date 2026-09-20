using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Models.Entities;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Số liệu tổng hợp của một ca — tính từ Payment và Invoice thuộc ca đó (BR-10, SCR-F09).</summary>
public class ShiftTotals
{
    public decimal OpeningCash { get; set; }

    /// <summary>Tiền mặt thu vào (các khoản dương, phương thức tiền mặt).</summary>
    public decimal CashIn { get; set; }

    /// <summary>Tiền mặt hoàn ra (âm).</summary>
    public decimal CashRefund { get; set; }

    /// <summary>Quỹ đầu ca + thu tiền mặt − hoàn tiền mặt.</summary>
    public decimal ExpectedCash { get; set; }

    public decimal BankTransferIn { get; set; }
    public decimal CardIn { get; set; }

    public int PaymentCount { get; set; }
    public int InvoiceCount { get; set; }

    // Doanh thu theo nguồn — SCR-F09
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal SurchargeRevenue { get; set; }
    public decimal CancellationRevenue { get; set; }

    /// <summary>Tổng hoàn tiền mọi phương thức (âm).</summary>
    public decimal RefundTotal { get; set; }
}

/// <summary>Một dòng ca trong danh sách ca gần đây — SCR-F08.</summary>
public class ShiftListItem
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public ShiftStatus Status { get; set; }
    public decimal? CashDifference { get; set; }
}

public class OpenShiftForm
{
    [Display(Name = "Quỹ tiền mặt đầu ca")]
    [Range(0, double.MaxValue, ErrorMessage = "Quỹ đầu ca không được âm.")]
    public decimal OpeningCash { get; set; }
}

public class CloseShiftForm
{
    public int ShiftId { get; set; }

    [Display(Name = "Tiền mặt đếm thực tế")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền đếm không được âm.")]
    public decimal CountedCash { get; set; }

    [Display(Name = "Lý do chênh lệch")]
    [MaxLength(500)]
    public string? DifferenceReason { get; set; }
}

/// <summary>Màn hình mở/đóng ca — SCR-F08.</summary>
public class ShiftIndexViewModel
{
    public CashierShift? OpenShift { get; set; }
    public ShiftTotals? OpenShiftTotals { get; set; }

    /// <summary>Ngưỡng chênh lệch tiền mặt để cảnh báo đỏ — SCR-F08.</summary>
    public decimal CashDifferenceThreshold { get; set; }

    public OpenShiftForm OpenForm { get; set; } = new();
    public CloseShiftForm CloseForm { get; set; } = new();

    public IReadOnlyList<ShiftListItem> RecentShifts { get; set; } = new List<ShiftListItem>();

    public bool IsAdmin { get; set; }
}

/// <summary>Một hóa đơn trong báo cáo cuối ca — SCR-F09.</summary>
public class ShiftInvoiceItem
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }
}

/// <summary>Báo cáo cuối ca — SCR-F09.</summary>
public class ShiftReportViewModel
{
    public CashierShift Shift { get; set; } = null!;
    public string EmployeeName { get; set; } = string.Empty;
    public ShiftTotals Totals { get; set; } = new();
    public IReadOnlyList<ShiftInvoiceItem> Invoices { get; set; } = new List<ShiftInvoiceItem>();
}
