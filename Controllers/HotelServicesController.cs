using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Dịch vụ khách sạn (ăn uống, minibar, giặt ủi, thuê xe...) — SCR-A06, SCR-A07.
[Authorize(Roles = Roles.All)]
public class HotelServicesController : AdminControllerBase
{
    private readonly IServiceCatalogService _service;

    public HotelServicesController(IServiceCatalogService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(HotelServiceIndexViewModel filter, int page = 1)
        => View(await _service.SearchAsync(filter, page));

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View(new HotelServiceFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePost(HotelServiceFormViewModel form)
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

        TempData["Success"] = $"Đã thêm dịch vụ {form.Code.ToUpperInvariant()}.";
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditPost(int id, HotelServiceFormViewModel form)
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

        TempData["Success"] = $"Đã cập nhật dịch vụ {form.Code}.";
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
            TempData["Success"] = "Đã ngừng sử dụng dịch vụ.";
            if (result.Warning is not null)
            {
                TempData["Warning"] = result.Warning;
            }
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
