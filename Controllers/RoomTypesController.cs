using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Loại phòng (giá, sức chứa, tiện nghi).
[Authorize(Roles = Roles.All)]
public class RoomTypesController : AdminControllerBase
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
}
