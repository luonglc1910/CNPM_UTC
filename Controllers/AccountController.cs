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
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Cắt bớt để không vượt AuditLog.UserName [MaxLength(100)] khi bị gửi chuỗi rác.
        var attemptedUserName = model.UserName.Length > 100
            ? model.UserName[..100]
            : model.UserName;

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.UserName == model.UserName);

        // Tài khoản không tồn tại vẫn phải tốn đúng chừng ấy thời gian băm, nếu không thì
        // đo thời gian đáp ứng là biết được tài khoản nào có thật.
        var passwordOk = employee is null
            ? PasswordHasher.Verify(model.Password, DummyHash) && false
            : PasswordHasher.Verify(model.Password, employee.PasswordHash);

        // SCR-S01: kiểm mật khẩu TRƯỚC, kiểm trạng thái SAU.
        // Docs vừa đòi thông báo giống hệt nhau, vừa đòi báo riêng khi tài khoản bị khóa —
        // hai điều đó chỉ dung hòa được khi thông báo riêng chỉ hiện cho người đã nhập đúng
        // mật khẩu. Kiểm trạng thái trước sẽ để lộ tài khoản nào tồn tại.
        if (!passwordOk)
        {
            if (employee is not null)
            {
                // Vẫn đếm số lần sai; việc tự khóa sau 5 lần (FR-A08) để đợt sau.
                employee.FailedLoginCount++;
                _audit.LogForUser(AuditActions.LoginFailed, attemptedUserName, employee,
                    $"Sai mật khẩu (lần thứ {employee.FailedLoginCount})");
            }
            else
            {
                _audit.LogForUser(AuditActions.LoginFailed, attemptedUserName, reason: "Tài khoản không tồn tại");
            }

            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(model);
        }

        if (employee!.Status == EmployeeStatus.Locked)
        {
            _audit.LogForUser(AuditActions.LoginFailed, attemptedUserName, employee, "Tài khoản đang bị khóa");
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Liên hệ quản lý.");
            return View(model);
        }

        if (employee.Status == EmployeeStatus.Resigned)
        {
            _audit.LogForUser(AuditActions.LoginFailed, attemptedUserName, employee, "Nhân viên đã nghỉ việc");
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "Tài khoản không còn hiệu lực.");
            return View(model);
        }

        employee.FailedLoginCount = 0;
        employee.LastLoginAt = DateTime.Now;
        _audit.LogForUser(AuditActions.LoginSucceeded, employee.UserName, employee);
        await _db.SaveChangesAsync();

        await SignInAsync(employee, model.RememberMe);

        if (employee.MustChangePassword)
        {
            TempData["Info"] = "Đây là lần đăng nhập đầu tiên, vui lòng đổi mật khẩu trước khi sử dụng hệ thống.";
            return RedirectToAction(nameof(ChangePassword));
        }

        // Url.IsLocalUrl bắt buộc phải kiểm — tránh bị lợi dụng chuyển hướng ra ngoài.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToHome(employee.Role);
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

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            CookieAuthenticationDefaults.AuthenticationScheme,
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
