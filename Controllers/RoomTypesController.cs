using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Loại phòng (giá, sức chứa, tiện nghi) — SCR-A01, SCR-A02.
[Authorize(Roles = Roles.All)]
public class RoomTypesController : AdminControllerBase
{
    private readonly IRoomTypeService _service;

    public RoomTypesController(IRoomTypeService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? keyword, bool includeInactive = false, int page = 1)
    {
        return View(new RoomTypeIndexViewModel
        {
            Keyword = keyword,
            IncludeInactive = includeInactive,
            Results = await _service.SearchAsync(keyword, includeInactive, page)
        });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View(new RoomTypeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePost(RoomTypeFormViewModel form)
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

        TempData["Success"] = $"Đã thêm loại phòng {form.Code.ToUpperInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy loại phòng.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditPost(int id, RoomTypeFormViewModel form)
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

        TempData["Success"] = $"Đã cập nhật loại phòng {form.Code}.";
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _service.DeactivateAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = "Đã ngừng sử dụng loại phòng.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
