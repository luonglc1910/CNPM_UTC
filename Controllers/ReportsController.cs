using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Báo cáo thống kê.
[Authorize(Roles = Roles.Admin)]
public class ReportsController : AdminControllerBase
{
    // Tổng doanh thu theo khoảng thời gian.
    public IActionResult Revenue() => View();

    // Tỷ lệ lấp đầy công suất phòng.
    public IActionResult Occupancy() => View();
}
