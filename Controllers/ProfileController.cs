using System.Security.Claims;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

[Authorize(Policy = "ClientOnly")]
public class ProfileController : Controller
{
    private readonly HotelDbContext _db;

    public ProfileController(HotelDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var guestId)) return RedirectToAction("Login", "ClientAuth");

        var guest = await _db.Guests.FindAsync(guestId);
        if (guest == null) return NotFound();

        var model = new ClientProfileViewModel
        {
            FullName = guest.FullName,
            PhoneNumber = guest.PhoneNumber,
            IdNumber = guest.IdNumber,
            DateOfBirth = guest.DateOfBirth,
            Gender = guest.Gender,
            Address = guest.Address
        };

        ViewData["RewardPoints"] = guest.RewardPoints;
        ViewData["Tier"] = guest.Tier;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ClientProfileViewModel model)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var guestId)) return RedirectToAction("Login", "ClientAuth");

        if (!ModelState.IsValid)
        {
            var g = await _db.Guests.FindAsync(guestId);
            if (g != null)
            {
                ViewData["RewardPoints"] = g.RewardPoints;
                ViewData["Tier"] = g.Tier;
            }
            return View(model);
        }

        var guest = await _db.Guests.FindAsync(guestId);
        if (guest == null) return NotFound();

        guest.FullName = model.FullName;
        guest.PhoneNumber = model.PhoneNumber;
        if (!string.IsNullOrEmpty(model.IdNumber)) guest.IdNumber = model.IdNumber;
        guest.DateOfBirth = model.DateOfBirth;
        guest.Gender = model.Gender;
        guest.Address = model.Address;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Cập nhật hồ sơ thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAjax([FromForm] ClientProfileViewModel model)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var guestId)) 
            return Json(ApiResponse.Fail("Vui lòng đăng nhập lại.", "AUTH_UNAUTHORIZED"));

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(ApiResponse.Fail(string.Join("<br/>", errors), "VALIDATION_ERROR"));
        }

        var guest = await _db.Guests.FindAsync(guestId);
        if (guest == null) 
            return Json(ApiResponse.Fail("Không tìm thấy thông tin tài khoản.", "USER_NOT_FOUND"));

        guest.FullName = model.FullName;
        guest.PhoneNumber = model.PhoneNumber;
        if (!string.IsNullOrEmpty(model.IdNumber)) guest.IdNumber = model.IdNumber;
        guest.DateOfBirth = model.DateOfBirth;
        guest.Gender = model.Gender;
        guest.Address = model.Address;

        try
        {
            await _db.SaveChangesAsync();
            return Json(ApiResponse.Ok("Cập nhật hồ sơ thành công!"));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Fail("Lỗi hệ thống khi lưu dữ liệu.", "DB_ERROR"));
        }
    }
}
