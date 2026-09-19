using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Báo cáo thống kê.
public class ReportsController : AdminControllerBase
{
    // Tổng doanh thu theo khoảng thời gian.
    public IActionResult Revenue() => View();

    // Tỷ lệ lấp đầy công suất phòng.
    public IActionResult Occupancy() => View();
}
