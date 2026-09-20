using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Báo cáo & quản trị — nhóm G. Chỉ Admin (ma trận phân quyền: nhóm G không mở cho lễ tân).
[Authorize(Roles = Roles.Admin)]
public class ReportsController : AdminControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    // SCR-G01 — doanh thu theo thời gian và theo nguồn.
    public async Task<IActionResult> Revenue(RevenueReportViewModel filter)
        => View(await _service.BuildRevenueAsync(filter));

    // SCR-G02 — công suất phòng theo ngày.
    public async Task<IActionResult> Occupancy(OccupancyReportViewModel filter)
        => View(await _service.BuildOccupancyAsync(filter));
}
