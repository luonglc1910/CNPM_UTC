using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

[Authorize(Roles = Roles.All)]
public class GalleryController : AdminControllerBase
{
    private readonly HotelDbContext _context;
    private readonly IWebHostEnvironment _env;

    public GalleryController(HotelDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _context.HotelGalleryImages
            .OrderBy(g => g.SortOrder)
            .ThenByDescending(g => g.Id)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(string? title, int sortOrder, IFormFile? imageFile, string? imageUrl)
    {
        string finalUrl = string.Empty;

        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "gallery");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            finalUrl = "/uploads/gallery/" + uniqueFileName;
        }
        else if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            finalUrl = imageUrl.Trim();
        }
        else
        {
            ModelState.AddModelError("", "Vui lòng tải lên tệp hình ảnh hoặc nhập URL hình ảnh.");
            return View();
        }

        var entity = new HotelGalleryImage
        {
            Title = title,
            ImageUrl = finalUrl,
            SortOrder = sortOrder,
            IsActive = true
        };

        _context.HotelGalleryImages.Add(entity);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã thêm hình ảnh vào thư viện.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var item = await _context.HotelGalleryImages.FindAsync(id);
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã {(item.IsActive ? "bật" : "tắt")} hiển thị hình ảnh.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.HotelGalleryImages.FindAsync(id);
        if (item != null)
        {
            _context.HotelGalleryImages.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hình ảnh khỏi thư viện.";
        }
        return RedirectToAction(nameof(Index));
    }
}
