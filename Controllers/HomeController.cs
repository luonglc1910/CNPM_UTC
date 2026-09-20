using System.Diagnostics;
using HotelManagement.Web.Models;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Trang tổng quan (Dashboard) và các trang hệ thống.
public class HomeController : Controller
{
    private readonly IAuditService _audit;
    private readonly IDashboardService _dashboard;

    public HomeController(IAuditService audit, IDashboardService dashboard)
    {
        _audit = audit;
        _dashboard = dashboard;
    }

    // SCR-S04 — tổng quan. Vai trò quyết định thẻ doanh thu hiển thị phạm vi nào,
    // và giới hạn đó được áp ngay trong truy vấn chứ không phải ẩn ở view.
    public async Task<IActionResult> Index()
        => View(await _dashboard.BuildAsync(
            User.GetEmployeeId() ?? 0,
            User.IsInRole(Roles.Admin)));

    /// <summary>
    /// Trang báo không có quyền — SCR-S03. Cookie authentication chuyển hướng tới đây
    /// qua AccessDeniedPath. Phải trả đúng mã 403, không được im lặng về trang chủ.
    /// </summary>
    public async Task<IActionResult> Forbidden(string? returnUrl = null)
    {
        // Ghi log để phát hiện hành vi dò quyền.
        await _audit.LogAndSaveAsync(AuditActions.AccessDenied, "Route", returnUrl);

        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["DeniedUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
