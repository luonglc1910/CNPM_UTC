using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Tồn kho và phiếu kho — SCR-A08, SCR-A09.
// Lễ tân chỉ xem tồn; nhập kho và điều chỉnh chỉ Admin.
[Authorize(Roles = Roles.All)]
public class InventoryController : AdminControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(InventoryIndexViewModel filter, int page = 1)
    {
        await _service.FillIndexAsync(filter, page);
        return View(filter);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Receive(int? hotelServiceId)
        => View(await _service.BuildReceiveFormAsync(hotelServiceId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Receive))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ReceivePost(InventoryReceiveViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.ServiceOptions = await _service.GetStockedServiceOptionsAsync();
            return View(form);
        }

        var result = await _service.ReceiveAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            form.ServiceOptions = await _service.GetStockedServiceOptionsAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Adjust(int? hotelServiceId)
        => View(await _service.BuildAdjustFormAsync(hotelServiceId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Adjust))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AdjustPost(InventoryAdjustViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.ServiceOptions = await _service.GetStockedServiceOptionsAsync();
            return View(form);
        }

        var result = await _service.AdjustAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            form.ServiceOptions = await _service.GetStockedServiceOptionsAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
