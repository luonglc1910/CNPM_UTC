using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Buồng phòng — nhóm E (SCR-E01, E03, E04). Lễ tân thao tác hộ nhân viên dọn phòng.
// SCR-E02 (kiểm minibar) thuộc luồng thu ngân, để module Billing xử lý.
[Authorize(Roles = Roles.All)]
public class HousekeepingController : AdminControllerBase
{
    private readonly IHousekeepingService _service;

    public HousekeepingController(IHousekeepingService service)
    {
        _service = service;
    }

    // SCR-E01
    public async Task<IActionResult> Index(HousekeepingBoardViewModel filter)
        => View(await _service.BuildBoardAsync(filter));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartCleaning(int roomId)
    {
        SetMessage(await _service.StartCleaningAsync(roomId, CurrentEmployeeId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCleaning(int roomId)
    {
        SetMessage(await _service.CompleteCleaningAsync(roomId, CurrentEmployeeId));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRepaired(int roomId)
    {
        SetMessage(await _service.MarkRepairedAsync(roomId, CurrentEmployeeId));
        return RedirectToAction(nameof(Index));
    }

    // SCR-E03
    [HttpGet]
    public async Task<IActionResult> CreateRequest(int? roomId)
        => View(await _service.BuildCreateRequestFormAsync(roomId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(CreateRequest))]
    public async Task<IActionResult> CreateRequestPost(CreateRequestViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await _service.FillRequestFormOptionsAsync(form);
            return View(form);
        }

        var result = await _service.CreateRequestAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await _service.FillRequestFormOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Requests));
    }

    // SCR-E04
    [HttpGet]
    public async Task<IActionResult> Requests(RequestListViewModel filter, int page = 1)
    {
        if (filter.HasFilter)
        {
            filter.CustomFilter = true;
        }

        return View(await _service.BuildRequestsAsync(filter, page));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRequest(int id, int assignTo)
    {
        SetMessage(await _service.AssignRequestAsync(id, assignTo, CurrentEmployeeId));
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRequest(int id, string? resolution, bool roomFixed)
    {
        SetMessage(await _service.CompleteRequestAsync(id, CurrentEmployeeId, resolution, roomFixed));
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id)
    {
        SetMessage(await _service.CancelRequestAsync(id, CurrentEmployeeId));
        return RedirectToAction(nameof(Requests));
    }

    // SCR-E02 — kiểm minibar: thuộc luồng thu ngân (module Billing), chưa triển khai ở đây.
    [HttpGet]
    public IActionResult MinibarUsage(int stayId) => Pending(nameof(Index));

    private int CurrentEmployeeId => User.GetEmployeeId() ?? 0;

    private void SetMessage(ServiceResult result)
    {
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return;
        }

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }
    }
}
