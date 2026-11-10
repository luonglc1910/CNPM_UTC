using System.Security.Claims;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using HotelManagement.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

/// <summary>
/// Đăng ký / Đăng nhập / Đăng xuất cho Khách hàng vãng lai trên Portal.
/// Dùng Scheme "ClientCookie" — hoàn toàn tách biệt với StaffAuth của nhân viên.
/// </summary>
public class ClientAuthController : Controller
{
    private readonly HotelDbContext _db;

    /// <summary>
    /// Băm giả để chống Timing Attack: dù email không tồn tại, server vẫn mất
    /// đúng bấy nhiêu thời gian như khi verify mật khẩu thật.
    /// </summary>
    private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");

    public ClientAuthController(HotelDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────── ĐĂNG KÝ ───────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (IsClientAuthenticated())
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Portal");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new ClientRegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(ClientRegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // Email phải là duy nhất — index DB đã bảo vệ nhưng báo lỗi thân thiện hơn ở đây.
        var emailTaken = await _db.Guests.AnyAsync(g => g.Email == model.Email);
        if (emailTaken)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng. Vui lòng đăng nhập hoặc dùng email khác.");
            return View(model);
        }

        var guest = new Guest
        {
            FullName    = model.FullName,
            Email       = model.Email,
            PhoneNumber = model.PhoneNumber,
            PasswordHash = PasswordHasher.Hash(model.Password),
            IsActive    = true
        };

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();

        // Tự động đăng nhập cho khách hàng vừa đăng ký thành công
        await SignInClientAsync(guest, isPersistent: true);

        TempData["Success"] = "Đăng ký tài khoản thành công!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Portal");
    }

    // ─────────────────────────── ĐĂNG NHẬP ───────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (IsStaffAuthenticated())
            return RedirectToAction("Index", "Home");
        if (IsClientAuthenticated())
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Portal");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new ClientLoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(ClientLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // 1. Kiểm tra Nhân viên trước
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.UserName == model.Email);
        if (employee != null)
        {
            var empPasswordOk = employee.PasswordHash == null 
                ? PasswordHasher.Verify(model.Password, DummyHash) && false 
                : PasswordHasher.Verify(model.Password, employee.PasswordHash);
            
            if (empPasswordOk)
            {
                if (employee.Status != EmployeeStatus.Active)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản nhân viên bị khóa hoặc không còn hiệu lực.");
                    return View(model);
                }
                
                employee.LastLoginAt = DateTime.Now;
                await _db.SaveChangesAsync();
                
                await SignInStaffAsync(employee, model.RememberMe);
                
                if (employee.MustChangePassword)
                {
                    TempData["Info"] = "Đây là lần đăng nhập đầu tiên, vui lòng đổi mật khẩu trước khi sử dụng hệ thống.";
                    return RedirectToAction("ChangePassword", "Account");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                
                return RedirectToAction("Index", "Home");
            }
        }

        // 2. Nếu không phải nhân viên, kiểm tra Khách hàng
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Email == model.Email);

        // Chống Timing Attack
        var passwordOk = guest?.PasswordHash is null
            ? PasswordHasher.Verify(model.Password, DummyHash) && false
            : PasswordHasher.Verify(model.Password, guest.PasswordHash);

        if (!passwordOk)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (!guest!.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
            return View(model);
        }

        guest.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await SignInClientAsync(guest, model.RememberMe);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Portal");
    }

    // ─────────────────────────── ĐĂNG XUẤT ───────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AppSchemes.Client);
        await HttpContext.SignOutAsync(AppSchemes.Staff);
        return RedirectToAction("Index", "Portal");
    }

    // ─────────────────────────── TIỆN ÍCH ───────────────────────────

    private bool IsStaffAuthenticated()
        => User.Identity?.IsAuthenticated == true
           && User.Identity.AuthenticationType == AppSchemes.Staff;

    private bool IsClientAuthenticated()
        => User.Identity?.IsAuthenticated == true
           && User.Identity.AuthenticationType == AppSchemes.Client;

    private async Task SignInClientAsync(Guest guest, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guest.Id.ToString()),
            new(ClaimTypes.Name,           guest.Email ?? string.Empty),
            new(ClaimTypes.GivenName,      guest.FullName ?? guest.Email ?? string.Empty)
        };

        var identity   = new ClaimsIdentity(claims, AppSchemes.Client);
        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc   = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(AppSchemes.Client, new ClaimsPrincipal(identity), properties);
    }

    private async Task SignInStaffAsync(Employee employee, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name,           employee.UserName),
            new(ClaimTypes.GivenName,      employee.FullName ?? employee.UserName),
            new(ClaimTypes.Role,           employee.Role.ToString())
        };

        if (employee.MustChangePassword)
        {
            claims.Add(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        var identity = new ClaimsIdentity(claims, AppSchemes.Staff);
        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddHours(8) : null
        };

        await HttpContext.SignInAsync(AppSchemes.Staff, new ClaimsPrincipal(identity), properties);
    }
}
