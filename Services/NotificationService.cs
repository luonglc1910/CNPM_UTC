using HotelManagement.Web.Data;
using HotelManagement.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

public class NotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = "/FrontDesk";
    public string Icon { get; set; } = "bell-fill";
    public string BadgeClass { get; set; } = "bg-primary";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

public interface INotificationService
{
    Task PushAsync(string type, string title, string message, string url = "/FrontDesk", string icon = "bell-fill", string badgeClass = "bg-primary");
    Task NotifyPaymentReceivedAsync(string refCode, decimal amount, string method, string url = "/Billing");
    Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int max = 15);
    int GetUnreadCount();
    void MarkAllRead();
}

public class NotificationService : INotificationService
{
    private static readonly List<NotificationItem> MemoryStore = new();
    private static readonly object SyncLock = new();

    private readonly IHubContext<NotificationHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationService(IHubContext<NotificationHub> hub, IServiceScopeFactory scopeFactory)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
    }

    public Task NotifyPaymentReceivedAsync(string refCode, decimal amount, string method, string url = "/Billing")
        => PushAsync("SettleInvoice", "Thanh toán thành công", $"Đã thu {amount:N0} ₫ ({method}) — {refCode}", url, "qr-code-scan", "bg-success");

    public async Task PushAsync(
        string type, string title, string message,
        string url = "/FrontDesk", string icon = "bell-fill", string badgeClass = "bg-primary")
    {
        var item = new NotificationItem
        {
            Type = type,
            Title = title,
            Message = message,
            Url = url,
            Icon = icon,
            BadgeClass = badgeClass,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        int unreadCount;
        lock (SyncLock)
        {
            MemoryStore.Insert(0, item);
            if (MemoryStore.Count > 50)
            {
                MemoryStore.RemoveRange(50, MemoryStore.Count - 50);
            }
            unreadCount = MemoryStore.Count(x => !x.IsRead);
        }

        await _hub.Clients.All.SendAsync("ReceiveNotification", new
        {
            id = item.Id,
            type = item.Type,
            title = item.Title,
            message = item.Message,
            url = item.Url,
            icon = item.Icon,
            badgeClass = item.BadgeClass,
            time = item.CreatedAt.ToString("HH:mm dd/MM"),
            unreadCount
        });
    }

    public async Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int max = 15)
    {
        List<NotificationItem> snapshot;
        lock (SyncLock)
        {
            snapshot = MemoryStore.Take(max).ToList();
        }

        if (snapshot.Count > 0)
        {
            return snapshot;
        }

        // Khởi tạo từ các sự kiện gần nhất trong DB nếu server vừa khởi động lại.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

        var recentLogs = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == "CreateReservation"
                     || a.Action == "PortalBooking"
                     || a.Action == "PortalQrPayment"
                     || a.Action == "InRoomRequest"
                     || a.Action == "CreateServiceRequest"
                     || a.Action == "SettleInvoice")
            .OrderByDescending(a => a.CreatedAt)
            .Take(max)
            .ToListAsync();

        var seeded = recentLogs.Select(a => new NotificationItem
        {
            Id = $"LOG-{a.Id}",
            Type = a.Action,
            Title = a.Action switch
            {
                "PortalBooking" => "Đặt phòng mới từ Portal",
                "PortalQrPayment" => "Thanh toán VietQR thành công",
                "InRoomRequest" => "Yêu cầu từ QR tại phòng",
                "CreateReservation" => "Đơn đặt phòng mới",
                "SettleInvoice" => "Thanh toán hóa đơn",
                _ => "Hoạt động hệ thống"
            },
            Message = a.NewValue ?? a.Reason ?? $"{a.EntityType} #{a.EntityId}",
            Url = a.EntityType == "Reservation" && !string.IsNullOrEmpty(a.EntityId)
                ? $"/Reservations/Details/{a.EntityId}"
                : a.EntityType == "ServiceRequest"
                    ? "/Housekeeping/Requests"
                    : "/FrontDesk",
            Icon = a.Action switch
            {
                "PortalQrPayment" or "SettleInvoice" => "qr-code-scan",
                "InRoomRequest" => "bell-fill",
                _ => "calendar-check"
            },
            BadgeClass = a.Action switch
            {
                "PortalQrPayment" => "bg-success",
                "InRoomRequest" => "bg-warning text-dark",
                _ => "bg-primary"
            },
            CreatedAt = a.CreatedAt,
            IsRead = true
        }).ToList();

        return seeded;
    }

    public int GetUnreadCount()
    {
        lock (SyncLock)
        {
            return MemoryStore.Count(x => !x.IsRead);
        }
    }

    public void MarkAllRead()
    {
        lock (SyncLock)
        {
            foreach (var item in MemoryStore)
            {
                item.IsRead = true;
            }
        }
    }
}
