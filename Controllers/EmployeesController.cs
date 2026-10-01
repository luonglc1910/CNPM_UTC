using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Nhân viên và tài khoản đăng nhập — SCR-A10, SCR-A11.
// Cả màn hình chỉ dành cho Admin; lễ tân vào là 403.
[Authorize(Roles = Roles.Admin)]
public class EmployeesController : AdminControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(EmployeeIndexViewModel filter, int page = 1)
        => View(await _service.SearchAsync(filter, page));

    [HttpGet]
    public IActionResult Create() => View(new EmployeeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public async Task<IActionResult> CreatePost(EmployeeFormViewModel form)
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

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(int id, EmployeeFormViewModel form)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            return View(await Refill(form));
        }

        var result = await _service.UpdateAsync(form);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorField ?? string.Empty, result.Error!);
            return View(await Refill(form));
        }

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var result = await _service.ToggleLockAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var result = await _service.ResetPasswordAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
            TempData["Warning"] = result.Warning;
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Form hiện lại sau lỗi phải giữ nguyên những thứ người dùng không nhập được:
    /// mã nhân viên, tên đăng nhập, cờ đang khóa / đang sửa chính mình.
    /// </summary>
    private async Task<EmployeeFormViewModel> Refill(EmployeeFormViewModel form)
    {
        var stored = await _service.GetForEditAsync(form.Id);
        if (stored is null)
        {
            return form;
        }

        form.Code = stored.Code;
        form.UserName = stored.UserName;
        form.IsLocked = stored.IsLocked;
        form.IsSelf = stored.IsSelf;
        form.HasOpenShift = stored.HasOpenShift;
        return form;
    }
}
