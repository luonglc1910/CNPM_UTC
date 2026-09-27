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

/// <summary>
/// Màn hình gộp SCR-G01 (doanh thu) và SCR-G02 (công suất) — hai tab dùng chung một
/// khoảng ngày, vì cả hai đều trả lời "kỳ này kinh doanh thế nào" và người xem
/// gần như luôn xem liền nhau.
/// </summary>
public class BusinessReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    [Display(Name = "Gộp theo")]
    public RevenuePeriod Period { get; set; } = RevenuePeriod.Day;

    public RevenueReportViewModel Revenue { get; set; } = new();
    public OccupancyReportViewModel Occupancy { get; set; } = new();
}

// ===================== SCR-G03 — Báo cáo dịch vụ & tồn kho =====================

/// <summary>Một dịch vụ trong bảng bán chạy — SCR-G03 tab 1.</summary>
public class ServiceSalesRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }

    /// <summary>Tỷ trọng doanh thu trên tổng, đơn vị phần trăm.</summary>
    public double Share { get; set; }
}

/// <summary>Doanh thu gộp theo nhóm dịch vụ — SCR-G03 tab 1.</summary>
public class CategorySalesRow
{
    public ServiceCategory Category { get; set; }
    public decimal Revenue { get; set; }
    public double Share { get; set; }
}

/// <summary>
/// Một dịch vụ có quản lý kho trong bảng đối chiếu — SCR-G03 tab 2.
///
/// Mọi <c>InventoryTransaction.Quantity</c> đều là số chênh có dấu (nhập dương, bán âm,
/// điều chỉnh +/−). Chốt kiểm soát của BR-12 là so tồn suy ra TỪ SỔ GIAO DỊCH với
/// <c>HotelService.StockQuantity</c> — con số hệ thống đang tin và dùng để chặn bán.
/// So sổ giao dịch với chính nó thì luôn khớp và không phát hiện được gì.
/// </summary>
public class StockReconciliationRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public int Opening { get; set; }
    public int Received { get; set; }

    /// <summary>Số lượng đã bán, quy về số dương để đọc.</summary>
    public int Sold { get; set; }

    public int Adjusted { get; set; }
    public int Returned { get; set; }

    /// <summary>Tồn hiện tại theo sổ cái dịch vụ — con số dùng để quyết định còn bán được không.</summary>
    public int BookStock { get; set; }

    public int MinStockLevel { get; set; }

    /// <summary>
    /// Kỳ báo cáo kéo tới hiện tại hay không. Chỉ khi đó mới so được sổ giao dịch với tồn
    /// hiện tại; kỳ đã khép lại trong quá khứ thì hai con số vốn dĩ khác nhau.
    /// </summary>
    public bool ComparableToBookStock { get; set; }

    /// <summary>Tồn cuối suy ra từ các giao dịch trong kỳ.</summary>
    public int Computed => Opening + Received - Sold + Adjusted + Returned;

    /// <summary>Chênh giữa tồn hệ thống đang tin và tồn suy từ giao dịch.</summary>
    public int Variance => BookStock - Computed;

    /// <summary>Sổ sách không khớp giao dịch — chốt kiểm soát cho BR-12.</summary>
    public bool Mismatch => ComparableToBookStock && Variance != 0;

    public bool IsOutOfStock => BookStock <= 0;
    public bool IsLow => !IsOutOfStock && BookStock <= MinStockLevel;
}

/// <summary>Báo cáo dịch vụ &amp; tồn kho — SCR-G03, FR-G05.</summary>
public class ServicesReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    public IReadOnlyList<ServiceSalesRow> Sales { get; set; } = new List<ServiceSalesRow>();
    public IReadOnlyList<CategorySalesRow> ByCategory { get; set; } = new List<CategorySalesRow>();
    public IReadOnlyList<StockReconciliationRow> Stock { get; set; } = new List<StockReconciliationRow>();

    public decimal TotalRevenue { get; set; }
    public int TotalQuantity { get; set; }

    public IEnumerable<StockReconciliationRow> MismatchRows => Stock.Where(s => s.Mismatch);
    public bool HasMismatch => MismatchRows.Any();

    /// <summary>Doanh thu lớn nhất một dòng, để vẽ thanh tỷ trọng.</summary>
    public decimal MaxRevenue => Sales.Count == 0 ? 0 : Sales.Max(s => s.Revenue);
}

// ===================== SCR-G04 — Báo cáo theo nhân viên / ca =====================

/// <summary>Một ca thu ngân đã mở trong khoảng — SCR-G04 tab 1.</summary>
public class ShiftSummaryRow
{
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public ShiftStatus Status { get; set; }

    public decimal Cash { get; set; }
    public decimal BankTransfer { get; set; }
    public decimal Card { get; set; }
    public decimal Total => Cash + BankTransfer + Card;

    /// <summary>Chênh lệch quỹ: đếm được − dự kiến. Ca chưa đóng thì chưa có.</summary>
    public decimal? CashDifference { get; set; }

    public string? DifferenceReason { get; set; }
    public bool HasDifference => CashDifference is not null && CashDifference != 0;
}

/// <summary>
/// Chênh lệch quỹ cộng dồn theo từng nhân viên — SCR-G04.
/// Dùng để phát hiện người thường xuyên lệch quỹ, không chỉ lệch một lần.
/// </summary>
public class StaffCashDifferenceRow
{
    public string EmployeeName { get; set; } = string.Empty;
    public int ShiftCount { get; set; }
    public int DifferenceCount { get; set; }
    public decimal TotalDifference { get; set; }

    /// <summary>Tổng trị tuyệt đối — hai ca lệch ±1 triệu không được triệt tiêu thành 0.</summary>
    public decimal TotalAbsoluteDifference { get; set; }
}

/// <summary>Thống kê thao tác của một nhân viên lễ tân — SCR-G04 tab 2.</summary>
public class ReceptionistActivityRow
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }

    public int CheckIns { get; set; }
    public int CheckOuts { get; set; }
    public int ReservationsCreated { get; set; }
    public int ReservationsCancelled { get; set; }
    public decimal TotalDiscount { get; set; }

    /// <summary>Số lần bấm tiếp dù hệ thống đã cảnh báo (khách hạn chế, bán âm kho).</summary>
    public int WarningOverrides { get; set; }
}

/// <summary>Báo cáo theo nhân viên / ca — SCR-G04, FR-G06.</summary>
public class StaffReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    [Display(Name = "Nhân viên")]
    public int? EmployeeId { get; set; }

    [Display(Name = "Vai trò")]
    public EmployeeRole? Role { get; set; }

    public IReadOnlyList<ShiftSummaryRow> Shifts { get; set; } = new List<ShiftSummaryRow>();
    public IReadOnlyList<StaffCashDifferenceRow> CashDifferences { get; set; } = new List<StaffCashDifferenceRow>();
    public IReadOnlyList<ReceptionistActivityRow> Activity { get; set; } = new List<ReceptionistActivityRow>();

    public IReadOnlyList<EmployeeOption> EmployeeOptions { get; set; } = new List<EmployeeOption>();

    public decimal TotalCollected => Shifts.Sum(s => s.Total);
    public int ShiftsWithDifference => Shifts.Count(s => s.HasDifference);
}

/// <summary>Một mục trong ô chọn nhân viên.</summary>
public class EmployeeOption
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
}

// ===================== SCR-G05 — Nhật ký thao tác =====================

/// <summary>Một dòng nhật ký — SCR-G05.</summary>
public class AuditLogRow
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public EmployeeRole? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }

    public bool HasValueChange => OldValue is not null || NewValue is not null;
}

/// <summary>
/// Nhật ký thao tác — SCR-G05, FR-G07, BR-11, NFR-07.
/// Màn hình chỉ đọc: bảng AuditLog chỉ ghi thêm, toàn ứng dụng không có chỗ nào sửa hay xóa.
/// </summary>
public class AuditLogReportViewModel : IDateRangeReport
{
    [Display(Name = "Từ ngày")]
    [DataType(DataType.Date)]
    public DateTime From { get; set; }

    [Display(Name = "Đến ngày")]
    [DataType(DataType.Date)]
    public DateTime To { get; set; }

    [Display(Name = "Người thực hiện")]
    public string? UserName { get; set; }

    // KHÔNG được đặt tên thuộc tính này là "Action": route mặc định có khóa {action},
    // model binding sẽ lấy luôn tên action ("AuditLog") nhét vào đây và màn hình luôn
    // lọc theo một hành động không tồn tại, ra bảng rỗng mà không báo lỗi gì.
    [Display(Name = "Loại hành động")]
    public string? ActionType { get; set; }

    [Display(Name = "Mã đối tượng")]
    public string? EntityId { get; set; }

    public PagedList<AuditLogRow> Items { get; set; } = new();

    public IReadOnlyList<string> ActionOptions { get; set; } = new List<string>();
    public IReadOnlyList<string> UserOptions { get; set; } = new List<string>();
}
