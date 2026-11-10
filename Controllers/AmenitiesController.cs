using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

[Authorize(Roles = Roles.All)]
public class AmenitiesController : AdminControllerBase
{
    private readonly HotelDbContext _db;

    public AmenitiesController(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Amenities.OrderBy(x => x.Name);
        
        return View(new AmenityIndexViewModel
        {
            Results = await PagedList<Amenity>.CreateAsync(query, page, 50)
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new AmenityFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AmenityFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var amenity = new Amenity
        {
            Name = form.Name,
            IconSvg = form.IconSvg
        };

        _db.Amenities.Add(amenity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm tiện nghi thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity == null) return NotFound();

        return View("Form", new AmenityFormViewModel
        {
            Id = amenity.Id,
            Name = amenity.Name,
            IconSvg = amenity.IconSvg
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AmenityFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity == null) return NotFound();

        amenity.Name = form.Name;
        amenity.IconSvg = form.IconSvg;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật tiện nghi thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var amenity = await _db.Amenities.FindAsync(id);
        if (amenity != null)
        {
            _db.Amenities.Remove(amenity);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa tiện nghi.";
        }
        return RedirectToAction(nameof(Index));
    }
}
