using HotelManagement.Web.Models;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Phòng và trạng thái phòng — SCR-A03, SCR-A04, SCR-A05.
[Authorize(Roles = Roles.All)]
public class RoomsController : AdminControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(
        string? keyword, int? floor, int? roomTypeId, RoomStatus? status, string view = "table", int page = 1)
    {
        var vm = new RoomIndexViewModel
        {
            Keyword = keyword,
            Floor = floor,
            RoomTypeId = roomTypeId,
            Status = status,
            View = view
        };

        await _service.FillIndexAsync(vm, page);
        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create()
    {
        var form = new RoomFormViewModel();
        await FillOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePost(RoomFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        var result = await _service.CreateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await FillOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = $"Đã thêm phòng {form.RoomNumber}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy phòng.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EditPost(int id, RoomFormViewModel form)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(form);
            return View(form);
        }

        var result = await _service.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            await FillOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = $"Đã cập nhật phòng {form.RoomNumber}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// SCR-A05 — mở hộp thoại đổi trạng thái. Cả Admin và Lễ tân đều dùng được;
    /// tập trạng thái hợp lệ do RoomService quyết định theo trạng thái hiện tại.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UpdateStatus(int id)
    {
        var form = await _service.GetStatusFormAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy phòng.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(UpdateStatus))]
    public async Task<IActionResult> UpdateStatusPost(int id, RoomStatusFormViewModel form)
    {
        form.RoomId = id;

        var result = await _service.UpdateStatusAsync(form);

        if (!result.Succeeded)
        {
            // Nạp lại danh sách trạng thái hợp lệ rồi hiện lỗi ngay trên hộp thoại.
            var reloaded = await _service.GetStatusFormAsync(id);
            if (reloaded is null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Index));
            }

            reloaded.NewStatus = form.NewStatus;
            reloaded.Reason = form.Reason;
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            return View(reloaded);
        }

        TempData["Success"] = $"Đã chuyển phòng {form.RoomNumber} sang \"{form.NewStatus.ToDisplayName()}\".";
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Nạp lại ô chọn loại phòng khi trả form về vì lỗi nhập liệu.</summary>
    private async Task FillOptionsAsync(RoomFormViewModel form)
        => form.RoomTypeOptions = await _service.GetRoomTypeOptionsAsync();
}
