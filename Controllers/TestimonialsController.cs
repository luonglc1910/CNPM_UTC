using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

[Authorize(Roles = Roles.All)]
public class TestimonialsController : AdminControllerBase
{
    private readonly HotelDbContext _db;

    public TestimonialsController(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Testimonials.OrderByDescending(x => x.Id);
        
        return View(new TestimonialIndexViewModel
        {
            Results = await PagedList<Testimonial>.CreateAsync(query, page, 20)
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new TestimonialFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TestimonialFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var testm = new Testimonial
        {
            CustomerName = form.CustomerName,
            Content = form.Content,
            Rating = form.Rating,
            Source = form.Source
        };

        _db.Testimonials.Add(testm);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var testm = await _db.Testimonials.FindAsync(id);
        if (testm == null) return NotFound();

        return View("Form", new TestimonialFormViewModel
        {
            Id = testm.Id,
            CustomerName = testm.CustomerName,
            Content = testm.Content,
            Rating = testm.Rating,
            Source = testm.Source
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TestimonialFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var testm = await _db.Testimonials.FindAsync(id);
        if (testm == null) return NotFound();

        testm.CustomerName = form.CustomerName;
        testm.Content = form.Content;
        testm.Rating = form.Rating;
        testm.Source = form.Source;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var testm = await _db.Testimonials.FindAsync(id);
        if (testm != null)
        {
            _db.Testimonials.Remove(testm);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa đánh giá.";
        }
        return RedirectToAction(nameof(Index));
    }
}
