using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Hồ sơ khách — SCR-B01, SCR-B02, SCR-B03. Cả Admin và Lễ tân đều dùng.
[Authorize(Roles = Roles.All)]
public class GuestsController : AdminControllerBase
{
    private readonly IGuestService _service;
    private readonly IAuditService _audit;

    public GuestsController(IGuestService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(GuestIndexViewModel filter, int page = 1)
        => View(await _service.SearchAsync(filter, page));

    public async Task<IActionResult> Details(int id)
    {
        var vm = await _service.GetDetailsAsync(id);
        if (vm is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách.";
            return RedirectToAction(nameof(Index));
        }

        // Nhóm B yêu cầu ghi nhật ký ở mức ĐỌC: đây là màn hình duy nhất hiện số giấy tờ đầy đủ,
        // nên phải trả lời được câu "ai đã xem hồ sơ của khách nào, lúc nào".
        await _audit.LogAndSaveAsync("ViewGuestProfile", nameof(Guest), id.ToString());

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new GuestFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public async Task<IActionResult> CreatePost(GuestFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _service.CreateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            return View(form);
        }

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(int id, GuestFormViewModel form)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _service.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            return View(form);
        }

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Tra trùng hồ sơ khi người dùng rời ô Số giấy tờ / SĐT — SCR-B02.
    /// Trả JSON cho JavaScript; số giấy tờ trong kết quả luôn ở dạng che.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckDuplicate(string? idNumber, string? phoneNumber, int? excludeId)
        => Json(await _service.CheckDuplicateAsync(idNumber, phoneNumber, excludeId));
}
