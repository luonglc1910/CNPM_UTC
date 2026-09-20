using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>Báo cáo có lọc theo khoảng ngày — dùng chung cho doanh thu và công suất.</summary>
public interface IDateRangeReport
{
    DateTime From { get; set; }
    DateTime To { get; set; }
}

/// <summary>Cách gộp kỳ cho báo cáo doanh thu — SCR-G01.</summary>
public enum RevenuePeriod
{
    Day = 1,
    Month = 2,
    Year = 3
}

/// <summary>Một dòng doanh thu theo kỳ — SCR-G01.</summary>
public class RevenueRow
{
    public string Label { get; set; } = string.Empty;

    /// <summary>Khóa sắp xếp theo thời gian, không hiển thị.</summary>
    public DateTime SortKey { get; set; }

    public decimal Room { get; set; }
    public decimal Service { get; set; }
    public decimal Surcharge { get; set; }
    public decimal Discount { get; set; }
    public decimal Cancellation { get; set; }
    public decimal Vat { get; set; }

    /// <summary>Doanh thu thuần theo nguồn: phòng + dịch vụ + phụ thu − giảm giá + phí hủy.</summary>
    public decimal Net => Room + Service + Surcharge - Discount + Cancellation;
}

/// <summary>Báo cáo doanh thu — SCR-G01, FR-G01, FR-G02.</summary>
public class RevenueReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    [Display(Name = "Gộp theo")]
    public RevenuePeriod Period { get; set; } = RevenuePeriod.Day;

    public IReadOnlyList<RevenueRow> Rows { get; set; } = new List<RevenueRow>();

    public RevenueRow Total { get; set; } = new() { Label = "Tổng cộng" };

    /// <summary>Tổng đã gồm VAT (tiền khách thực trả trên các hóa đơn Settled) — chỉ để tham chiếu.</summary>
    public decimal GrossSettled { get; set; }

    public int InvoiceCount { get; set; }

    /// <summary>Giá trị lớn nhất trong cột Net để vẽ thanh tỷ lệ.</summary>
    public decimal MaxNet => Rows.Count == 0 ? 0 : Rows.Max(r => r.Net);
}

/// <summary>Một ngày trong báo cáo công suất — SCR-G02.</summary>
public class OccupancyRow
{
    public DateTime Date { get; set; }
    public int OccupiedRooms { get; set; }
    public int UsableRooms { get; set; }

    public double Rate => UsableRooms == 0 ? 0 : (double)OccupiedRooms / UsableRooms;
}

/// <summary>Báo cáo công suất phòng — SCR-G03, FR-G03.</summary>
public class OccupancyReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    public int UsableRooms { get; set; }

    public IReadOnlyList<OccupancyRow> Rows { get; set; } = new List<OccupancyRow>();

    /// <summary>Tổng số phòng-đêm đã bán trong khoảng.</summary>
    public int SoldRoomNights { get; set; }

    /// <summary>Tổng số phòng-đêm có thể bán = số phòng khai thác được × số đêm.</summary>
    public int CapacityRoomNights { get; set; }

    public double AverageRate => CapacityRoomNights == 0 ? 0 : (double)SoldRoomNights / CapacityRoomNights;

    /// <summary>Bị cắt bớt vì khoảng quá dài (giới hạn số ngày hiển thị).</summary>
    public bool Truncated { get; set; }
}
