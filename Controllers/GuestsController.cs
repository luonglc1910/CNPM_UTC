using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Hồ sơ khách — SCR-B01, SCR-B02, SCR-B03. Cả Admin và Lễ tân đều dùng.
[Authorize(Roles = Roles.All)]
public class GuestsController : AdminControllerBase
{
    private readonly IGuestService _service;
    private readonly IAuditService _audit;

    public GuestsController(IGuestService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    /// <summary>
    /// SCR-B01 (danh sách khách) và SCR-B04 (khai báo tạm trú) — hai tab của một màn.
    /// Chỉ tab đang mở mới nạp dữ liệu: mở tab tạm trú ghi một dòng nhật ký truy cập dữ liệu
    /// cá nhân, nạp sẵn cả hai thì mỗi lần xem danh sách cũng sinh nhật ký sai sự thật.
    /// </summary>
    public async Task<IActionResult> Index(
        GuestIndexViewModel filter, int page = 1, string? tab = null, DateTime? date = null)
    {
        var vm = new GuestsPageViewModel
        {
            Tab = tab == GuestsPageViewModel.ResidenceTab
                ? GuestsPageViewModel.ResidenceTab
                : GuestsPageViewModel.ListTab
        };

        if (vm.IsResidence)
        {
            vm.Residence = await _service.BuildResidenceAsync(date);
        }
        else
        {
            vm.List = await _service.SearchAsync(filter, page);
        }

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var vm = await _service.GetDetailsAsync(id);
        if (vm is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách.";
            return RedirectToAction(nameof(Index));
        }

        // Nhóm B yêu cầu ghi nhật ký ở mức ĐỌC: đây là màn hình duy nhất hiện số giấy tờ đầy đủ,
        // nên phải trả lời được câu "ai đã xem hồ sơ của khách nào, lúc nào".
        await _audit.LogAndSaveAsync("ViewGuestProfile", nameof(Guest), id.ToString());

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new GuestFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public async Task<IActionResult> CreatePost(GuestFormViewModel form)
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
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var form = await _service.GetForEditAsync(id);
        if (form is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách.";
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public async Task<IActionResult> EditPost(int id, GuestFormViewModel form)
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

        TempData["Success"] = result.Message;
        if (result.Warning is not null)
        {
            TempData["Warning"] = result.Warning;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Tra trùng hồ sơ khi người dùng rời ô Số giấy tờ / SĐT — SCR-B02.
    /// Trả JSON cho JavaScript; số giấy tờ trong kết quả luôn ở dạng che.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckDuplicate(string? idNumber, string? phoneNumber, int? excludeId)
        => Json(await _service.CheckDuplicateAsync(idNumber, phoneNumber, excludeId));

    /// <summary>URL cũ của SCR-B04 trước khi gộp vào SCR-B01. Giữ để link đã lưu không gãy.</summary>
    [HttpGet]
    public IActionResult Residence(DateTime? date)
        // Truyền ngày dưới dạng yyyy-MM-dd chứ không để DateTime tự định dạng: chuỗi
        // "09/15/2026 00:00:00" trong query string sẽ đè lên giá trị model, và <input type="date">
        // không đọc được định dạng đó nên ô chọn ngày hiện ra trống.
        => RedirectToAction(nameof(Index), new
        {
            tab = GuestsPageViewModel.ResidenceTab,
            date = date?.ToString("yyyy-MM-dd")
        });

    /// <summary>
    /// Đưa vào / gỡ khỏi danh sách hạn chế — SCR-B05. Chỉ Admin: đây là quyết định ảnh hưởng
    /// tới khách, lễ tân chỉ được thấy cảnh báo chứ không được tự đặt.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Blacklist(int id, BlacklistForm form)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            // Hộp thoại nằm trên trang chi tiết nên lỗi cũng quay về đó; gửi kèm lỗi đầu tiên
            // vì modal không giữ lại được ModelState sau redirect.
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _service.SetBlacklistAsync(form);
        SetMessage(result);
        return RedirectToAction(nameof(Details), new { id });
    }

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
