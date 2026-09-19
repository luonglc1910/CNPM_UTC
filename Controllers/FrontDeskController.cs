using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Lễ tân: check-in, check-out và quản lý khách đang lưu trú.
public class FrontDeskController : AdminControllerBase
{
    // Danh sách phòng đang có khách lưu trú.
    public IActionResult Index() => View();

    // Check-in cho khách đã đặt phòng trước.
    [HttpGet]
    public IActionResult CheckIn(int? reservationId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(CheckIn))]
    public IActionResult CheckInPost(int? reservationId) => Pending(nameof(Index));

    // Check-in cho khách vãng lai.
    [HttpGet]
    public IActionResult WalkIn() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(WalkIn))]
    public IActionResult WalkInPost() => Pending(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckOut(int stayId) => Pending(nameof(Index));

    [HttpGet]
    public IActionResult ChangeRoom(int stayId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(ChangeRoom))]
    public IActionResult ChangeRoomPost(int stayId) => Pending(nameof(Index));

    // Khai báo thêm khách ở ghép.
    [HttpGet]
    public IActionResult AddGuest(int stayId) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(AddGuest))]
    public IActionResult AddGuestPost(int stayId) => Pending(nameof(Index));
}
