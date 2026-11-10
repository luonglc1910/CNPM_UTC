using HotelManagement.Web.Models;
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
    private readonly IEmailService _email;

    public BillingController(IBillingService service, IEmailService email)
    {
        _service = service;
        _email = email;
    }

    // SCR-F01
    public async Task<IActionResult> Index(BillingIndexViewModel filter, int page = 1)
        => View(await _service.BuildIndexAsync(filter, page));

    // SCR-F02
    public async Task<IActionResult> Folio(int stayId)
    {
        var vm = await _service.GetFolioAsync(stayId);
        return vm is null
            ? Blocked(BlockedReason.Stay(await _service.GetStayStatusAsync(stayId), "mở folio"), nameof(Index))
            : View(vm);
    }

    // SCR-F03
    [HttpGet]
    public async Task<IActionResult> AddCharge(int stayId)
    {
        var vm = await _service.BuildAddChargeAsync(stayId);
        return vm is null
            ? Blocked(BlockedReason.Stay(await _service.GetStayStatusAsync(stayId), "thêm chi phí"), nameof(Index))
            : View(vm);
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
                return Blocked(
                    BlockedReason.Stay(await _service.GetStayStatusAsync(form.StayId), "thêm chi phí"),
                    nameof(Index));
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
        return vm is null
            ? Blocked(BlockedReason.Stay(await _service.GetStayStatusAsync(stayId), "giảm giá"), nameof(Index))
            : View(vm);
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
        return vm is null
            ? Blocked(BlockedReason.Stay(await _service.GetStayStatusAsync(stayId), "thanh toán"), nameof(Index))
            : View(vm);
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
        return vm is null
            ? Blocked(BlockedReason.Invoice(await _service.GetInvoiceStatusAsync(id), "xem"), nameof(Index))
            : View(vm);
    }

    // SCR-F07
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> VoidInvoice(int id)
    {
        var vm = await _service.BuildVoidAsync(id, CurrentEmployeeId);
        return vm is null
            ? Blocked(BlockedReason.Invoice(await _service.GetInvoiceStatusAsync(id), "hủy"), nameof(Index))
            : View(vm);
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
        return vm is null
            ? Blocked(BlockedReason.Stay(await _service.GetStayStatusAsync(stayId), "kiểm minibar"), nameof(Index))
            : View(vm);
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

    // ---------- Chuyển / Gộp chi phí liên phòng & Thanh toán gộp (Master Folio / Group Checkout) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferFolio(
        int fromStayId, int toStayId, int? folioItemId, bool includeRoomCharges = false, string reason = "")
    {
        var result = await _service.TransferFolioAsync(
            fromStayId, toStayId, folioItemId, includeRoomCharges, reason, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(Folio), new { stayId = fromStayId });
    }

    [HttpGet]
    public async Task<IActionResult> GroupPayment(int? reservationId = null, string? stayIds = null)
    {
        var vm = await _service.BuildGroupPaymentAsync(reservationId, stayIds, CurrentEmployeeId, IsAdmin);
        if (vm is null)
        {
            TempData["Error"] = "Không tìm thấy phòng đang lưu trú trong nhóm/đoàn để thanh toán gộp.";
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrepareGroupCheckout(int? reservationId, string? stayIdsCsv)
    {
        var result = await _service.PrepareGroupCheckoutAsync(reservationId, stayIdsCsv, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(GroupPayment), new { reservationId, stayIds = stayIdsCsv });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(GroupPayment))]
    public async Task<IActionResult> GroupPaymentPost(GroupPaymentViewModel form)
    {
        var (result, firstInvoiceId) = await _service.PayGroupAsync(form, CurrentEmployeeId, IsAdmin);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(GroupPayment), new { reservationId = form.ReservationId, stayIds = form.StayIdsCsv });
        }

        TempData["Success"] = result.Message;
        return firstInvoiceId > 0
            ? RedirectToAction(nameof(Invoice), new { id = firstInvoiceId })
            : RedirectToAction(nameof(Index), new { IsInvoiceTab = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SettleDebt(int invoiceId, PaymentMethod method, string? transactionRef)
    {
        var result = await _service.SettleDebtAsync(invoiceId, method, transactionRef, CurrentEmployeeId);
        SetMessage(result);
        return RedirectToAction(nameof(Invoice), new { id = invoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvoiceEmail(int invoiceId)
    {
        var email = await _email.SendInvoiceEmailAsync(invoiceId);
        if (email.Sent)
        {
            TempData["Success"] = "Đã gửi email hóa đơn điện tử cho khách và lưu bản xem trước!";
        }
        else
        {
            TempData["Error"] = $"Chưa gửi được email hóa đơn: {email.Message}";
        }
        if (!string.IsNullOrWhiteSpace(email.PreviewUrl))
            TempData["EmailPreviewUrl"] = email.PreviewUrl;

        return RedirectToAction(nameof(Invoice), new { id = invoiceId });
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
