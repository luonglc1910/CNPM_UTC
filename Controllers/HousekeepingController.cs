using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Buồng phòng & dịch vụ: trạng thái dọn dẹp, yêu cầu phục vụ, minibar.
[Authorize(Roles = Roles.All)]
public class HousekeepingController : AdminControllerBase
{
    // Bảng trạng thái dọn dẹp của tất cả các phòng.
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int roomId) => Pending(nameof(Index));

    // Các yêu cầu phục vụ phòng chưa xử lý.
    public IActionResult Requests() => View();

    [HttpGet]
    public IActionResult CreateRequest() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(CreateRequest))]
    public IActionResult CreateRequestPost() => Pending(nameof(Requests));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteRequest(int id) => Pending(nameof(Requests));

    // Ghi nhận sử dụng minibar để tính tiền vào hóa đơn.
    [HttpGet]
    public IActionResult MinibarUsage(int roomId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(MinibarUsage))]
    public IActionResult MinibarUsagePost(int roomId) => Pending(nameof(Index));
}
