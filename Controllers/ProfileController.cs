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

        var currentPoints = guest.RewardPoints;
        ViewData["RewardPoints"] = currentPoints;
        
        string tierName = guest.Tier switch {
            HotelManagement.Web.Models.MemberTier.Diamond => "Diamond",
            HotelManagement.Web.Models.MemberTier.Gold => "Gold",
            HotelManagement.Web.Models.MemberTier.Silver => "Silver",
            _ => "Standard"
        };
        ViewData["Tier"] = tierName;

        int nextTierPoints = guest.Tier switch {
            HotelManagement.Web.Models.MemberTier.Diamond => 500, // max
            HotelManagement.Web.Models.MemberTier.Gold => 500,
            HotelManagement.Web.Models.MemberTier.Silver => 200,
            _ => 50
        };

        int prevTierPoints = guest.Tier switch {
            HotelManagement.Web.Models.MemberTier.Diamond => 500,
            HotelManagement.Web.Models.MemberTier.Gold => 200,
            HotelManagement.Web.Models.MemberTier.Silver => 50,
            _ => 0
        };

        int pointsNeeded = nextTierPoints - currentPoints;
        if (pointsNeeded < 0) pointsNeeded = 0;
        
        int range = nextTierPoints - prevTierPoints;
        int currentProgress = currentPoints - prevTierPoints;
        if (currentProgress < 0) currentProgress = 0;

        int percent = range == 0 ? 100 : (int)Math.Round((double)currentProgress / range * 100);
        if (percent > 100) percent = 100;

        ViewData["ProgressPercent"] = percent;
        ViewData["PointsNeeded"] = guest.Tier == HotelManagement.Web.Models.MemberTier.Diamond ? 0 : pointsNeeded;

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
                var currentPoints = g.RewardPoints;
                ViewData["RewardPoints"] = currentPoints;
                
                string tierName = g.Tier switch {
                    HotelManagement.Web.Models.MemberTier.Diamond => "Diamond",
                    HotelManagement.Web.Models.MemberTier.Gold => "Gold",
                    HotelManagement.Web.Models.MemberTier.Silver => "Silver",
                    _ => "Standard"
                };
                ViewData["Tier"] = tierName;

                int nextTierPoints = g.Tier switch {
                    HotelManagement.Web.Models.MemberTier.Diamond => 500,
                    HotelManagement.Web.Models.MemberTier.Gold => 500,
                    HotelManagement.Web.Models.MemberTier.Silver => 200,
                    _ => 50
                };

                int prevTierPoints = g.Tier switch {
                    HotelManagement.Web.Models.MemberTier.Diamond => 500,
                    HotelManagement.Web.Models.MemberTier.Gold => 200,
                    HotelManagement.Web.Models.MemberTier.Silver => 50,
                    _ => 0
                };

                int pointsNeeded = nextTierPoints - currentPoints;
                if (pointsNeeded < 0) pointsNeeded = 0;
                
                int range = nextTierPoints - prevTierPoints;
                int currentProgress = currentPoints - prevTierPoints;
                if (currentProgress < 0) currentProgress = 0;

                int percent = range == 0 ? 100 : (int)Math.Round((double)currentProgress / range * 100);
                if (percent > 100) percent = 100;

                ViewData["ProgressPercent"] = percent;
                ViewData["PointsNeeded"] = g.Tier == HotelManagement.Web.Models.MemberTier.Diamond ? 0 : pointsNeeded;
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
        catch (Exception)
        {
            return Json(ApiResponse.Fail("Lỗi hệ thống khi lưu dữ liệu.", "DB_ERROR"));
        }
    }
}
