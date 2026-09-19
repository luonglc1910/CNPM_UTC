using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Web.Controllers;

// Danh mục: Nhân viên.
public class EmployeesController : AdminControllerBase
{
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Create))]
    public IActionResult CreatePost() => Pending(nameof(Index));

    [HttpGet]
    public IActionResult Edit(int id) => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Edit))]
    public IActionResult EditPost(int id) => Pending(nameof(Index));
}
