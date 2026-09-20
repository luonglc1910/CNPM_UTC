using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Phòng và trạng thái sẵn sàng / bảo trì.
[Authorize(Roles = Roles.All)]
public class RoomsController : AdminControllerBase
{
    public IActionResult Index() => View();

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult CreatePost() => Pending(nameof(Index));

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Edit(int id) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult EditPost(int id) => Pending(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    // TODO SCR-A05: cả hai vai trò đều được chuyển mọi trạng thái, trừ một luật duy nhất —
    // phòng đang Occupied không ai được sửa tay, chỉ đổi qua check-out hoặc đổi phòng.
    // Luật này phải kiểm trong thân action khi làm nghiệp vụ thật.
    public IActionResult UpdateStatus(int id) => Pending(nameof(Index));
}
