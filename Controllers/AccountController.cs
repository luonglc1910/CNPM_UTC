using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Đăng nhập / đăng xuất cho Admin.
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password, string? returnUrl = null)
    {
        // TODO: kiểm tra tài khoản Admin, tạo ClaimsPrincipal và gọi HttpContext.SignInAsync.
        TempData["Info"] = "Chức năng đăng nhập chưa được triển khai.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
