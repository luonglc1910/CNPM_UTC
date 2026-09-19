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
}
