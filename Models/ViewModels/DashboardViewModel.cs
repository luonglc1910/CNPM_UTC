namespace HotelManagement.Web.Models.ViewModels;

/// <summary>
/// Một việc đang tồn, hiện ở khối "Việc cần xử lý" — SCR-S04.
/// Số 0 vẫn hiện, để người trực thấy rõ là đã kiểm và không còn gì, khác với "chưa biết".
/// </summary>
public class PendingWorkItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Icon { get; set; } = string.Empty;

    /// <summary>Màn hình xử lý việc này.</summary>
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Tham số kèm theo khi điều hướng, ví dụ mở đúng tab.
    /// Kiểu từ điển vì tag helper asp-all-route-data nhận đúng kiểu này.
    /// </summary>
    public Dictionary<string, string> RouteValues { get; set; } = new();

    /// <summary>Câu giải thích ngắn khi có việc tồn; để trống khi Count = 0.</summary>
    public string? Hint { get; set; }
}

/// <summary>
/// Dashboard tổng quan — SCR-S04, FR-G09.
///
/// Chỉ gồm con số và việc cần xử lý, không lặp lại bảng khách đến / khách đi: hai bảng đó
/// là tab 1 và tab 2 của SCR-D01 (bảng điều khiển lễ tân) và ở đó mới có nút Check-in /
/// Check-out. Mỗi con số ở đây bấm được, dẫn thẳng sang màn hình xử lý tương ứng.
/// </summary>
public class DashboardViewModel
{
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int MaintenanceRooms { get; set; }

    public int ArrivalsToday { get; set; }
    public int DeparturesToday { get; set; }

    /// <summary>
    /// Doanh thu hôm nay. Admin thấy toàn khách sạn; lễ tân chỉ thấy phần thu trong ca
    /// của mình — lọc ở tầng truy vấn theo <c>Invoice.CashierShiftId</c> chứ không ẩn ở view.
    /// </summary>
    public decimal RevenueToday { get; set; }

    /// <summary>Đúng nội dung thẻ doanh thu đang hiển thị, để nhãn nói thật.</summary>
    public bool RevenueIsWholeHotel { get; set; }

    /// <summary>Lễ tân chưa mở ca thì không có ca nào để cộng doanh thu.</summary>
    public bool HasOpenShift { get; set; }

    public IReadOnlyList<PendingWorkItem> PendingWork { get; set; } = new List<PendingWorkItem>();

    public int TotalPending => PendingWork.Sum(p => p.Count);

    /// <summary>Số phòng còn khai thác được, làm mẫu số cho tỉ lệ lấp đầy.</summary>
    public int UsableRooms { get; set; }

    public double OccupancyRate => UsableRooms == 0 ? 0 : (double)OccupiedRooms / UsableRooms;
}
