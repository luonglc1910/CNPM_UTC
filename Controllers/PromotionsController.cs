using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

[Authorize(Roles = Roles.All)]
public class PromotionsController : AdminControllerBase
{
    private readonly HotelDbContext _db;

    public PromotionsController(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Promotions.OrderByDescending(x => x.Id);
        
        return View(new PromotionIndexViewModel
        {
            Results = await PagedList<Promotion>.CreateAsync(query, page, 20)
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new PromotionFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PromotionFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var promo = new Promotion
        {
            Title = form.Title,
            Description = form.Description,
            ImageUrl = form.ImageUrl,
            DisplayPrice = form.DisplayPrice,
            StartDate = form.StartDate,
            EndDate = form.EndDate,
            IsHeroOffer = form.IsHeroOffer
        };

        _db.Promotions.Add(promo);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm ưu đãi thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var promo = await _db.Promotions.FindAsync(id);
        if (promo == null) return NotFound();

        return View("Form", new PromotionFormViewModel
        {
            Id = promo.Id,
            Title = promo.Title,
            Description = promo.Description,
            ImageUrl = promo.ImageUrl,
            DisplayPrice = promo.DisplayPrice,
            StartDate = promo.StartDate,
            EndDate = promo.EndDate,
            IsHeroOffer = promo.IsHeroOffer
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PromotionFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var promo = await _db.Promotions.FindAsync(id);
        if (promo == null) return NotFound();

        promo.Title = form.Title;
        promo.Description = form.Description;
        promo.ImageUrl = form.ImageUrl;
        promo.DisplayPrice = form.DisplayPrice;
        promo.StartDate = form.StartDate;
        promo.EndDate = form.EndDate;
        promo.IsHeroOffer = form.IsHeroOffer;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật ưu đãi thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await _db.Promotions.FindAsync(id);
        if (promo != null)
        {
            _db.Promotions.Remove(promo);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa ưu đãi.";
        }
        return RedirectToAction(nameof(Index));
    }
}
