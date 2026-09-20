using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Thu ngân — nhóm F (SCR-F01…F07) + kiểm minibar (E02). Hủy hóa đơn chỉ Admin.
[Authorize(Roles = Roles.All)]
public class BillingController : AdminControllerBase
{
    private readonly IBillingService _service;

    public BillingController(IBillingService service)
    {
        _service = service;
    }

    // SCR-F01
    public async Task<IActionResult> Index(BillingIndexViewModel filter, int page = 1)
        => View(await _service.BuildIndexAsync(filter, page));

    // SCR-F02
    public async Task<IActionResult> Folio(int stayId)
    {
        var vm = await _service.GetFolioAsync(stayId);
        return vm is null ? NotFound() : View(vm);
    }

    // SCR-F03
    [HttpGet]
    public async Task<IActionResult> AddCharge(int stayId)
    {
        var vm = await _service.BuildAddChargeAsync(stayId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(AddCharge))]
    public async Task<IActionResult> AddChargePost(AddChargeViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var reload = await _service.BuildAddChargeAsync(form.StayId);
            if (reload is null)
            {
                return NotFound();
            }

            reload.ItemType = form.ItemType;
            reload.HotelServiceId = form.HotelServiceId;
            reload.Quantity = form.Quantity;
            reload.UnitPrice = form.UnitPrice;
            reload.Description = form.Description;
            return View(reload);
        }

        var result = await _service.AddChargeAsync(form, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(Folio), new { stayId = form.StayId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidLine(int folioItemId, int stayId, string reason)
    {
        var result = await _service.VoidLineAsync(folioItemId, reason, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(Folio), new { stayId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockFolio(int stayId)
    {
        SetMessage(await _service.LockFolioAsync(stayId, CurrentEmployeeId));
        return RedirectToAction(nameof(Folio), new { stayId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UnlockFolio(int stayId)
    {
        SetMessage(await _service.UnlockFolioAsync(stayId, CurrentEmployeeId));
        return RedirectToAction(nameof(Folio), new { stayId });
    }

    // SCR-F04
    [HttpGet]
    public async Task<IActionResult> Discount(int stayId)
    {
        var vm = await _service.BuildDiscountAsync(stayId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Discount))]
    public async Task<IActionResult> DiscountPost(DiscountViewModel form)
    {
        var result = await _service.ApplyDiscountAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Discount), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Folio), new { stayId = form.StayId });
    }

    // SCR-F05
    [HttpGet]
    public async Task<IActionResult> Payment(int stayId)
    {
        var vm = await _service.BuildPaymentAsync(stayId, CurrentEmployeeId, IsAdmin);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Payment))]
    public async Task<IActionResult> PaymentPost(PaymentViewModel form)
    {
        var (result, invoiceId) = await _service.PayAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Payment), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Invoice), new { id = invoiceId });
    }

    // SCR-F06
    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var vm = await _service.BuildInvoiceAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    // SCR-F07
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> VoidInvoice(int id)
    {
        var vm = await _service.BuildVoidAsync(id, CurrentEmployeeId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(VoidInvoice))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> VoidInvoicePost(VoidInvoiceViewModel form)
    {
        var result = await _service.VoidInvoiceAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(VoidInvoice), new { id = form.InvoiceId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Invoice), new { id = form.InvoiceId });
    }

    // E02 — kiểm phòng & minibar
    [HttpGet]
    public async Task<IActionResult> Minibar(int stayId)
    {
        var vm = await _service.BuildMinibarAsync(stayId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Minibar))]
    public async Task<IActionResult> MinibarPost(MinibarViewModel form)
    {
        var result = await _service.SaveMinibarAsync(form, CurrentEmployeeId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Minibar), new { stayId = form.StayId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction("CheckOut", "FrontDesk", new { stayId = form.StayId });
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
