using System.Security.Claims;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

/// <summary>Đăng nhập, đăng xuất, đổi mật khẩu — SCR-S01, SCR-S02.</summary>
public class AccountController : Controller
{
    /// <summary>
    /// Thông báo chung cho mọi lỗi tài khoản/mật khẩu — SCR-S01 yêu cầu không được để lộ
    /// tài khoản nào tồn tại qua nội dung thông báo.
    /// </summary>
    private const string InvalidCredentialsMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";

    /// <summary>
    /// Hash giả để chạy Verify khi không tìm thấy tài khoản. Không có bước này thì phản hồi
    /// nhanh hơn hẳn và lộ ra tài khoản nào tồn tại qua thời gian đáp ứng.
    /// </summary>
    private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");

    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;

    public AccountController(HotelDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return RedirectToAction("Login", "ClientAuth", new { returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult Login()
    {
        return RedirectToAction("Login", "ClientAuth");
    }

    // AllowAnonymous để phiên đã hết hạn vẫn bấm Đăng xuất được thay vì bị đá về trang đăng nhập.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await _audit.LogAndSaveAsync(AuditActions.Logout, nameof(Employee),
                User.GetEmployeeId()?.ToString());
        }

        await HttpContext.SignOutAsync(AppSchemes.Staff);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel { IsForced = User.MustChangePassword() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        model.IsForced = User.MustChangePassword();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var employeeId = User.GetEmployeeId();
        var employee = employeeId is null
            ? null
            : await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null)
        {
            // Cookie còn nhưng tài khoản không còn: buộc đăng nhập lại.
            await HttpContext.SignOutAsync(AppSchemes.Staff);
            return RedirectToAction(nameof(Login));
        }

        if (!PasswordHasher.Verify(model.CurrentPassword, employee.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        if (PasswordHasher.Verify(model.NewPassword, employee.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(model);
        }

        employee.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        employee.MustChangePassword = false;

        // Không bao giờ ghi giá trị mật khẩu vào nhật ký.
        _audit.Log(AuditActions.PasswordChanged, nameof(Employee), employee.Id.ToString());
        await _db.SaveChangesAsync();

        // Phát hành lại cookie để bỏ claim MustChangePassword, nếu không filter vẫn chặn.
        await SignInAsync(employee, isPersistent: false);

        TempData["Success"] = "Đổi mật khẩu thành công.";
        return RedirectToHome(employee.Role);
    }

    /// <summary>Tạo ClaimsPrincipal và ghi cookie đăng nhập.</summary>
    private async Task SignInAsync(Employee employee, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.UserName),
            new(ClaimTypes.GivenName, employee.FullName),
            // Phải là TÊN enum, vì [Authorize(Roles = "Admin")] so sánh chuỗi.
            new(ClaimTypes.Role, employee.Role.ToString())
        };

        if (employee.MustChangePassword)
        {
            claims.Add(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            // Ghi nhớ đăng nhập: 7 ngày. Không ghi nhớ: dùng ExpireTimeSpan 8 giờ ở Program.cs.
            ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(
            AppSchemes.Staff,
            new ClaimsPrincipal(identity),
            properties);
    }

    /// <summary>Trang chủ mặc định theo vai trò — SCR-S01.</summary>
    private IActionResult RedirectToHome(EmployeeRole? role = null)
    {
        role ??= Enum.TryParse<EmployeeRole>(User.GetRole(), out var parsed) ? parsed : null;

        return role switch
        {
            EmployeeRole.Receptionist => RedirectToAction("Index", "FrontDesk"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
