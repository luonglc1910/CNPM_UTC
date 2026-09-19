using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Đặt phòng: Admin/lễ tân tạo và quản lý đơn đặt phòng thay cho khách
// (không có cổng đặt phòng cho khách hàng).
public class ReservationsController : AdminControllerBase
{
    // Danh sách đơn đặt phòng, lọc theo ngày / trạng thái / khách sắp đến.
    public IActionResult Index() => View();

    // Tra cứu phòng trống theo khoảng ngày và loại phòng.
    public IActionResult Availability() => View();

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public IActionResult CreatePost() => Pending(nameof(Index));

    public IActionResult Details(int id) => View();

    [HttpGet]
    public IActionResult Edit(int id) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public IActionResult EditPost(int id) => Pending(nameof(Details), new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id) => Pending(nameof(Details), new { id });
}
