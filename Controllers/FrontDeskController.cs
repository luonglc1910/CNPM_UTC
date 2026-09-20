using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Lễ tân — nhóm D (SCR-D01…D08). Admin và lễ tân quyền như nhau.
[Authorize(Roles = Roles.All)]
public class FrontDeskController : AdminControllerBase
{
    private readonly IFrontDeskService _service;

    public FrontDeskController(IFrontDeskService service)
    {
        _service = service;
    }

    // SCR-D01
    public async Task<IActionResult> Index()
        => View(await _service.BuildDashboardAsync());

    // SCR-D02
    [HttpGet]
    public async Task<IActionResult> CheckIn(int reservationId)
    {
        var vm = await _service.BuildCheckInAsync(reservationId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(CheckIn))]
    public async Task<IActionResult> CheckInPost(CheckInViewModel form)
    {
        var result = await _service.CheckInAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(CheckIn), new { reservationId = form.ReservationId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // SCR-D03
    [HttpGet]
    public async Task<IActionResult> WalkIn()
        => View(await _service.BuildWalkInAsync(CurrentEmployeeId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(WalkIn))]
    public async Task<IActionResult> WalkInPost(WalkInViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await _service.FillWalkInOptionsAsync(form, CurrentEmployeeId);
            return View(form);
        }

        var (result, stayId) = await _service.WalkInAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await _service.FillWalkInOptionsAsync(form, CurrentEmployeeId);
            return View(form);
        }

        SetMessage(result);
        return RedirectToAction(nameof(Stay), new { id = stayId });
    }

    // SCR-D04
    [HttpGet]
    public async Task<IActionResult> Stay(int id)
    {
        var vm = await _service.GetStayAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    // SCR-D05
    [HttpGet]
    public async Task<IActionResult> AddGuest(int stayId)
    {
        var vm = await _service.BuildAddGuestAsync(stayId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(AddGuest))]
    public async Task<IActionResult> AddGuestPost(AddGuestViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var reload = await _service.BuildAddGuestAsync(form.StayId);
            if (reload is null)
            {
                return NotFound();
            }

            reload.FullName = form.FullName;
            reload.IdNumber = form.IdNumber;
            reload.IsChild = form.IsChild;
            return View(reload);
        }

        var result = await _service.AddGuestAsync(form, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(Stay), new { id = form.StayId });
    }

    // SCR-D06
    [HttpGet]
    public async Task<IActionResult> ChangeRoom(int stayId)
    {
        var vm = await _service.BuildChangeRoomAsync(stayId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(ChangeRoom))]
    public async Task<IActionResult> ChangeRoomPost(ChangeRoomViewModel form)
    {
        var result = await _service.ChangeRoomAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(ChangeRoom), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Stay), new { id = form.StayId });
    }

    // SCR-D07
    [HttpGet]
    public async Task<IActionResult> Extend(int stayId)
    {
        var vm = await _service.BuildExtendAsync(stayId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Extend))]
    public async Task<IActionResult> ExtendPost(ExtendStayViewModel form)
    {
        var result = await _service.ExtendAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Extend), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Stay), new { id = form.StayId });
    }

    // SCR-D08
    [HttpGet]
    public async Task<IActionResult> CheckOut(int stayId)
    {
        var vm = await _service.BuildCheckOutAsync(stayId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(CheckOut))]
    public async Task<IActionResult> CheckOutPost(CheckOutViewModel form)
    {
        var result = await _service.CheckOutAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(CheckOut), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction("Payment", "Billing", new { stayId = form.StayId });
    }

    private int CurrentEmployeeId => User.GetEmployeeId() ?? 0;

    private bool IsAdmin => User.IsInRole(Roles.Admin);

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
