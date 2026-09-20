using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Lớp cơ sở cho các controller quản lý. Pending() dùng tạm cho các action POST
// chưa có nghiệp vụ: báo cho người dùng rồi quay về trang chỉ định.
public abstract class AdminControllerBase : Controller
{
    protected IActionResult Pending(string actionName, object? routeValues = null)
    {
        TempData["Info"] = "Chức năng này chưa được triển khai.";
        return RedirectToAction(actionName, routeValues);
    }

    /// <summary>
    /// Không mở được màn hình vì bản ghi không tồn tại hoặc đang ở trạng thái không cho thao tác.
    ///
    /// Dùng thay cho <c>NotFound()</c>: 404 trắng khiến người dùng tưởng đường dẫn hỏng, trong khi
    /// bản ghi vẫn còn đó và chỉ đang ở trạng thái khác. Quy ước của dự án là báo lỗi nghiệp vụ
    /// bằng TempData rồi chuyển trang (00-conventions.md mục 4).
    /// </summary>
    protected IActionResult Blocked(
        string message, string actionName, string? controllerName = null, object? routeValues = null)
    {
        TempData["Error"] = message;
        return controllerName is null
            ? RedirectToAction(actionName, routeValues)
            : RedirectToAction(actionName, controllerName, routeValues);
    }
}
