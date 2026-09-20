using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Ca làm việc thu ngân — SCR-F08, SCR-F09, BR-10.
// Admin xem/đóng được mọi ca; lễ tân chỉ ca của mình.
[Authorize(Roles = Roles.All)]
public class ShiftsController : AdminControllerBase
{
    private readonly IShiftService _service;

    public ShiftsController(IShiftService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
        => View(await _service.BuildIndexAsync(CurrentEmployeeId, IsAdmin));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open([Bind(Prefix = "OpenForm")] OpenShiftForm form)
    {
        var result = await _service.OpenAsync(CurrentEmployeeId, form.OpeningCash);
        SetResultMessage(result);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close([Bind(Prefix = "CloseForm")] CloseShiftForm form)
    {
        var result = await _service.CloseAsync(form.ShiftId, CurrentEmployeeId, IsAdmin, form);
        SetResultMessage(result);

        return result.Succeeded
            ? RedirectToAction(nameof(Report), new { id = form.ShiftId })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        var vm = await _service.BuildReportAsync(id, CurrentEmployeeId, IsAdmin);
        if (vm is null)
        {
            return Forbid();
        }

        return View(vm);
    }

    private int CurrentEmployeeId => User.GetEmployeeId() ?? 0;

    private bool IsAdmin => User.IsInRole(Roles.Admin);

    private void SetResultMessage(ServiceResult result)
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
