using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Đặt phòng — nhóm C (SCR-C01…C09). Admin và lễ tân quyền như nhau;
// miễn phí hủy (WaiveFee ở SCR-C08) chỉ Admin.
[Authorize(Roles = Roles.All)]
public class ReservationsController : AdminControllerBase
{
    private readonly IReservationService _service;

    public ReservationsController(IReservationService service)
    {
        _service = service;
    }

    // SCR-C01
    public async Task<IActionResult> Index(ReservationIndexViewModel filter, int page = 1)
    {
        if (filter.HasFilter)
        {
            filter.CustomFilter = true;
        }

        return View(await _service.BuildIndexAsync(filter, page));
    }

    // SCR-C02
    [HttpGet]
    public async Task<IActionResult> Availability(AvailabilitySearchViewModel vm)
    {
        await _service.BuildAvailabilityAsync(vm);
        return View(vm);
    }

    // SCR-C04
    [HttpGet]
    public async Task<IActionResult> Create(DateTime? checkIn, DateTime? checkOut, int? roomTypeId, int? roomId)
        => View(await _service.BuildCreateFormAsync(checkIn, checkOut, roomTypeId, roomId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public async Task<IActionResult> CreatePost(ReservationFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await _service.FillFormOptionsAsync(form);
            return View(form);
        }

        var (result, id) = await _service.CreateAsync(form, IsAdmin);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await _service.FillFormOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    // SCR-C05
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _service.GetDetailsAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    // SCR-C06
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.BuildEditFormAsync(id);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(ReservationFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await _service.FillFormOptionsAsync(form);
            return View(form);
        }

        var (result, id) = await _service.UpdateAsync(form, IsAdmin);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await _service.FillFormOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    // SCR-C07
    [HttpGet]
    public async Task<IActionResult> Deposit(int id)
    {
        var form = await _service.BuildDepositFormAsync(id, CurrentEmployeeId);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Deposit))]
    public async Task<IActionResult> DepositPost(DepositFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var reload = await _service.BuildDepositFormAsync(form.ReservationId, CurrentEmployeeId);
            if (reload is null)
            {
                return NotFound();
            }

            reload.Amount = form.Amount;
            reload.Method = form.Method;
            reload.TransactionRef = form.TransactionRef;
            reload.Notes = form.Notes;
            return View(reload);
        }

        var result = await _service.TakeDepositAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Deposit), new { id = form.ReservationId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = form.ReservationId });
    }

    // SCR-C08
    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var vm = await _service.BuildCancelAsync(id, CurrentEmployeeId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Cancel))]
    public async Task<IActionResult> CancelPost(CancelReservationViewModel form)
    {
        var result = await _service.CancelAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Cancel), new { id = form.ReservationId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = form.ReservationId });
    }

    // SCR-C09
    [HttpGet]
    // SCR-C03 — ma trận phòng × ngày, nhìn nhanh khoảng trống để lấp.
    [HttpGet]
    public async Task<IActionResult> RoomChart(DateTime? from, int days = 14)
        => View(await _service.BuildRoomChartAsync(from, days));

    public async Task<IActionResult> NoShow()
        => View(await _service.BuildNoShowListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var result = await _service.MarkNoShowAsync(id, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(NoShow));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtendHold(int id, int hours)
    {
        var result = await _service.ExtendHoldAsync(id, hours, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(NoShow));
    }

    private int CurrentEmployeeId => User.GetEmployeeId() ?? 0;

    private bool IsAdmin => User.IsInRole(Roles.Admin);

    private void SetMessage(ServiceResult result)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
    }
}
