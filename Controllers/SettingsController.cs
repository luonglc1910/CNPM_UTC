using HotelManagement.Web.Data;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Cấu hình hệ thống — SCR-A12. Chỉ Admin; lễ tân vào là 403.
[Authorize(Roles = Roles.Admin)]
public class SettingsController : AdminControllerBase
{
    /// <summary>Định dạng ảnh chấp nhận cho logo và phần mở rộng tương ứng.</summary>
    private static readonly Dictionary<string, string> AllowedLogoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    private const long MaxLogoBytes = 1024 * 1024;

    private readonly ISettingsService _service;
    private readonly IWebHostEnvironment _environment;

    public SettingsController(ISettingsService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    public async Task<IActionResult> Index() => View(await _service.GetAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHotel(
        [Bind(Prefix = "Hotel")] HotelInfoSettings form, IFormFile? logo, bool removeLogo = false)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.Hotel, vm => vm.Hotel = form);
        }

        string? newLogoPath = null;

        if (logo is { Length: > 0 })
        {
            var saved = await SaveLogoAsync(logo);
            if (!saved.Succeeded)
            {
                ModelState.AddModelError("logo", saved.Error!);
                return await Reload(SystemSettingDefaults.Groups.Hotel, vm => vm.Hotel = form);
            }

            newLogoPath = saved.Message;
        }

        // Bỏ logo thì xóa luôn file, không để lại ảnh mồ côi trong wwwroot/uploads.
        if (removeLogo && newLogoPath is null)
        {
            DeleteLogoFiles();
        }

        var result = await _service.SaveHotelAsync(form, newLogoPath, removeLogo);
        return await Finish(result, SystemSettingDefaults.Groups.Hotel, vm => vm.Hotel = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCheckInOut([Bind(Prefix = "CheckInOut")] CheckInOutSettings form)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.CheckInOut, vm => vm.CheckInOut = form);
        }

        var result = await _service.SaveCheckInOutAsync(form);
        return await Finish(result, SystemSettingDefaults.Groups.CheckInOut, vm => vm.CheckInOut = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTax([Bind(Prefix = "Tax")] TaxSettings form)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.Tax, vm => vm.Tax = form);
        }

        var result = await _service.SaveTaxAsync(form);
        return await Finish(result, SystemSettingDefaults.Groups.Tax, vm => vm.Tax = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSurcharge([Bind(Prefix = "Surcharge")] SurchargeSettings form)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.Surcharge, vm => vm.Surcharge = form);
        }

        var result = await _service.SaveSurchargeAsync(form);
        return await Finish(result, SystemSettingDefaults.Groups.Surcharge, vm => vm.Surcharge = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCancellation([Bind(Prefix = "Cancellation")] CancellationSettings form)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.Cancellation, vm => vm.Cancellation = form);
        }

        var result = await _service.SaveCancellationAsync(form);
        return await Finish(result, SystemSettingDefaults.Groups.Cancellation, vm => vm.Cancellation = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLimit([Bind(Prefix = "Limit")] LimitSettings form)
    {
        if (!ModelState.IsValid)
        {
            return await Reload(SystemSettingDefaults.Groups.Limit, vm => vm.Limit = form);
        }

        var result = await _service.SaveLimitAsync(form);
        return await Finish(result, SystemSettingDefaults.Groups.Limit, vm => vm.Limit = form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetGroup(string group)
    {
        var result = await _service.ResetGroupAsync(group);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { group });
    }

    /// <summary>Lưu xong: thành công thì chuyển hướng, thất bại thì hiện lại đúng nhóm vừa sửa.</summary>
    private async Task<IActionResult> Finish(ServiceResult result, string group, Action<SettingsViewModel> keepPosted)
    {
        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                result.ErrorField is null ? string.Empty : $"{group}.{result.ErrorField}",
                result.Error!);

            return await Reload(group, keepPosted);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { group });
    }

    /// <summary>
    /// Nạp lại năm nhóm còn lại từ DB và giữ nguyên giá trị người dùng vừa gõ ở nhóm bị lỗi —
    /// không thì sửa sai một ô là mất hết những gì đã nhập.
    /// </summary>
    private async Task<IActionResult> Reload(string group, Action<SettingsViewModel> keepPosted)
    {
        var vm = await _service.GetAsync();
        keepPosted(vm);
        vm.ActiveGroup = group;
        return View(nameof(Index), vm);
    }

    /// <summary>
    /// Ghi file logo vào wwwroot/uploads. Trả về đường dẫn tương đối trong <c>Message</c>.
    /// Tên file cố định theo phần mở rộng nên mỗi lần đổi logo là ghi đè, không tích rác.
    /// </summary>
    private async Task<ServiceResult> SaveLogoAsync(IFormFile logo)
    {
        if (logo.Length > MaxLogoBytes)
        {
            return ServiceResult.Fail("Logo tối đa 1 MB.");
        }

        if (!AllowedLogoTypes.TryGetValue(logo.ContentType, out var extension))
        {
            return ServiceResult.Fail("Logo phải là ảnh PNG, JPG hoặc WEBP.");
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);

        // Không dùng tên file người dùng gửi lên — tránh cả path traversal lẫn đuôi file giả.
        var fileName = $"hotel-logo{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await logo.CopyToAsync(stream);
        }

        // Dọn file logo định dạng khác còn sót lại để không còn hai file logo cùng lúc.
        DeleteLogoFiles(except: extension);

        return ServiceResult.Ok(message: $"/uploads/{fileName}");
    }

    private void DeleteLogoFiles(string? except = null)
    {
        var folder = Path.Combine(_environment.WebRootPath, "uploads");

        foreach (var extension in AllowedLogoTypes.Values.Distinct().Where(e => e != except))
        {
            var path = Path.Combine(folder, $"hotel-logo{extension}");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
