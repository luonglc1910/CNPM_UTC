using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using HotelManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Controllers;

/// <summary>
/// Cổng Self-Service (QR In-Room Service) dành cho khách đang lưu trú quét mã QR tại phòng (Chức năng 3).
/// </summary>
[AllowAnonymous]
public class InRoomController : Controller
{
    private readonly HotelDbContext _db;
    private readonly IBillingService _billing;
    private readonly INumberSequenceService _numbers;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ISettingsReader _settings;

    public InRoomController(
        HotelDbContext db,
        IBillingService billing,
        INumberSequenceService numbers,
        INotificationService notifications,
        IAuditService audit,
        ISettingsReader settings)
    {
        _db = db;
        _billing = billing;
        _numbers = numbers;
        _notifications = notifications;
        _audit = audit;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Menu(int? roomId, string? roomNumber)
    {
        Room? room = null;
        if (roomId.HasValue)
        {
            room = await _db.Rooms.AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == roomId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(roomNumber))
        {
            room = await _db.Rooms.AsNoTracking()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber.Trim());
        }
        else
        {
            room = await _db.Rooms.AsNoTracking()
                .Include(r => r.RoomType)
                .OrderBy(r => r.RoomNumber)
                .FirstOrDefaultAsync(r => r.IsActive);
        }

        if (room is null)
        {
            return NotFound("Không tìm thấy thông tin phòng.");
        }

        var activeStay = await _db.Stays.AsNoTracking()
            .Include(s => s.PrimaryGuest)
            .FirstOrDefaultAsync(s => s.RoomId == room.Id && s.Status == StayStatus.CheckedIn);

        BillingFolioSummary? folioSummary = null;
        if (activeStay is not null)
        {
            folioSummary = await _billing.ComputeFolioSummaryAsync(activeStay.Id);
        }

        var services = await _db.HotelServices.AsNoTracking()
            .Where(s => s.IsActive && (!s.IsStockManaged || s.StockQuantity > 0))
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToListAsync();

        var recentRequests = await _db.ServiceRequests.AsNoTracking()
            .Where(sr => sr.RoomId == room.Id)
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.Room = room;
        ViewBag.ActiveStay = activeStay;
        ViewBag.FolioSummary = folioSummary;
        ViewBag.Services = services;
        ViewBag.RecentRequests = recentRequests;
        ViewBag.HotelName = await _settings.GetStringAsync(SystemSettingKeys.HotelName);
        ViewBag.HotelPhone = await _settings.GetStringAsync(SystemSettingKeys.HotelPhone);

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> OrderService(int roomId, int serviceId, int quantity = 1, string? note = null)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);

        if (room is null || service is null)
        {
            TempData["Error"] = "Không tìm thấy phòng hoặc dịch vụ.";
            return RedirectToAction(nameof(Menu), new { roomId });
        }

        if (quantity < 1) quantity = 1;

        var activeStay = await _db.Stays
            .Include(s => s.Folio)
            .FirstOrDefaultAsync(s => s.RoomId == room.Id && s.Status == StayStatus.CheckedIn);

        // Nếu phòng đang có khách CheckedIn và Folio mở, tự động ghi vào Folio và trừ tồn kho
        string folioNote = "Đã gửi yêu cầu tới quầy lễ tân.";
        if (activeStay is not null && activeStay.Folio is not null && !activeStay.Folio.IsLocked)
        {
            var chargeRes = await _billing.AddChargeAsync(new AddChargeViewModel
            {
                StayId = activeStay.Id,
                ItemType = FolioItemType.Service,
                HotelServiceId = service.Id,
                Quantity = quantity,
                UnitPrice = service.UnitPrice,
                Description = service.Name
            }, employeeId: 1);

            if (!chargeRes.Succeeded)
            {
                TempData["Error"] = chargeRes.Error;
                return RedirectToAction(nameof(Menu), new { roomId });
            }
            folioNote = $"Đã tự động ghi vào Folio phòng {room.RoomNumber} ({(service.UnitPrice * quantity):N0} ₫).";
        }

        var seq = await _numbers.NextValueAsync("REQ", string.Empty);
        var reqCode = $"REQ-{seq:D6}";
        var desc = $"[QR Phòng {room.RoomNumber}] Gọi {quantity} {service.Unit} {service.Name}" +
                   (!string.IsNullOrWhiteSpace(note) ? $" — Ghi chú: {note.Trim()}" : "");

        _db.ServiceRequests.Add(new ServiceRequest
        {
            Code = reqCode,
            Type = RequestType.GuestService,
            RoomId = room.Id,
            Description = desc,
            Priority = RequestPriority.Medium,
            Status = RequestStatus.New
        });

        _audit.Log("InRoomRequest", nameof(ServiceRequest), reqCode, newValue: desc);
        await _db.SaveChangesAsync();

        await _notifications.PushAsync(
            "InRoomRequest",
            $"P.{room.RoomNumber} gọi dịch vụ (QR)",
            $"{desc}. {folioNote}",
            "/Housekeeping/Requests",
            "cup-hot-fill",
            "bg-warning text-dark");

        TempData["Success"] = $"Đã đặt {quantity} {service.Unit} {service.Name} thành công! Nhân viên đang chuẩn bị phục vụ lên phòng {room.RoomNumber}.";
        return RedirectToAction(nameof(Menu), new { roomId });
    }

    [HttpPost]
    public async Task<IActionResult> QuickRequest(int roomId, RequestType type, RequestPriority priority, string presetTitle, string? extraNote)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            return NotFound();
        }

        var seq = await _numbers.NextValueAsync("REQ", string.Empty);
        var reqCode = $"REQ-{seq:D6}";
        var desc = $"[QR Phòng {room.RoomNumber}] {presetTitle.Trim()}" +
                   (!string.IsNullOrWhiteSpace(extraNote) ? $" — Chi tiết: {extraNote.Trim()}" : "");

        if (desc.Length < 10)
        {
            desc += " (Yêu cầu từ khách tại phòng)";
        }

        _db.ServiceRequests.Add(new ServiceRequest
        {
            Code = reqCode,
            Type = type,
            RoomId = room.Id,
            Description = desc,
            Priority = priority,
            Status = RequestStatus.New
        });

        _audit.Log("InRoomRequest", nameof(ServiceRequest), reqCode, newValue: desc);
        await _db.SaveChangesAsync();

        await _notifications.PushAsync(
            "InRoomRequest",
            type == RequestType.Maintenance ? $"P.{room.RoomNumber} báo sự cố (QR)" : $"P.{room.RoomNumber} yêu cầu phục vụ (QR)",
            desc,
            "/Housekeeping/Requests",
            type == RequestType.Maintenance ? "tools" : "bell-fill",
            type == RequestType.Maintenance ? "bg-danger" : "bg-info text-dark");

        TempData["Success"] = $"Đã gửi yêu cầu \"{presetTitle}\" ({reqCode}) tới quầy lễ tân!";
        return RedirectToAction(nameof(Menu), new { roomId });
    }
}

