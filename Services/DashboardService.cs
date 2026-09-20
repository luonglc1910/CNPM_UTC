using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Số liệu cho dashboard tổng quan — SCR-S04, FR-G09.
///
/// Đứng riêng thay vì nhét vào một service có sẵn: dashboard đọc chéo sáu bảng
/// (Rooms, Reservations, Stays, Invoices, ServiceRequests, HotelServices) nên gắn vào
/// bất kỳ service nào cũng kéo service đó ra ngoài trách nhiệm của nó.
/// </summary>
public interface IDashboardService
{
    Task<DashboardViewModel> BuildAsync(int employeeId, bool isAdmin);
}

/// <inheritdoc />
public class DashboardService : IDashboardService
{
    private readonly HotelDbContext _db;
    private readonly IShiftService _shifts;

    public DashboardService(HotelDbContext db, IShiftService shifts)
    {
        _db = db;
        _shifts = shifts;
    }

    public async Task<DashboardViewModel> BuildAsync(int employeeId, bool isAdmin)
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var roomStatuses = await _db.Rooms.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Status)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            AvailableRooms = roomStatuses.Count(s => s == RoomStatus.Available),
            OccupiedRooms = roomStatuses.Count(s => s == RoomStatus.Occupied),
            DirtyRooms = roomStatuses.Count(s => s == RoomStatus.Dirty),
            MaintenanceRooms = roomStatuses.Count(s => s == RoomStatus.Maintenance),
            // Mẫu số bỏ phòng ngừng khai thác, giống cách tính công suất ở SCR-G02.
            UsableRooms = roomStatuses.Count(s => s != RoomStatus.OutOfService)
        };

        vm.ArrivalsToday = await _db.Reservations.AsNoTracking()
            .CountAsync(r => r.Status == ReservationStatus.Confirmed
                && r.CheckInDate >= today && r.CheckInDate < tomorrow);

        vm.DeparturesToday = await _db.Stays.AsNoTracking()
            .CountAsync(s => s.Status == StayStatus.CheckedIn
                && s.ExpectedCheckOut >= today && s.ExpectedCheckOut < tomorrow);

        await FillRevenueAsync(vm, employeeId, isAdmin, today, tomorrow);
        vm.PendingWork = await BuildPendingWorkAsync();

        return vm;
    }

    /// <summary>
    /// Doanh thu hôm nay. Giới hạn của lễ tân làm ở tầng truy vấn (lọc theo ca) chứ không
    /// phải ẩn trên view — SCR-S04 nói rõ điều này.
    /// </summary>
    private async Task FillRevenueAsync(
        DashboardViewModel vm, int employeeId, bool isAdmin, DateTime today, DateTime tomorrow)
    {
        var settled = _db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Settled
                && i.IssuedAt >= today && i.IssuedAt < tomorrow);

        if (isAdmin)
        {
            vm.RevenueIsWholeHotel = true;
            vm.HasOpenShift = true;
            vm.RevenueToday = await settled.SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
            return;
        }

        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        vm.HasOpenShift = shift is not null;

        // Chưa mở ca thì không có ca nào để cộng vào — hiện 0 kèm nhắc mở ca, chứ không
        // âm thầm hiện doanh thu của người khác.
        vm.RevenueToday = shift is null
            ? 0m
            : await settled.Where(i => i.CashierShiftId == shift.Id)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;
    }

    private async Task<List<PendingWorkItem>> BuildPendingWorkAsync()
    {
        var dirty = await _db.Rooms.AsNoTracking()
            .CountAsync(r => r.IsActive && r.Status == RoomStatus.Dirty);

        // Đơn đã xác nhận, ngày đến đã tới hoặc đã qua, quá giờ giữ chỗ mà khách chưa đến.
        // Cùng tiêu chí với SCR-C09 để hai chỗ không nói hai con số khác nhau.
        var now = DateTime.Now;
        var overdue = await _db.Reservations.AsNoTracking()
            .CountAsync(r => r.Status == ReservationStatus.Confirmed
                && r.CheckInDate < now.Date.AddDays(1)
                && r.HoldUntil != null && r.HoldUntil < now);

        var openRequests = await _db.ServiceRequests.AsNoTracking()
            .CountAsync(r => r.Status == RequestStatus.New || r.Status == RequestStatus.InProgress);

        var lowStock = await _db.HotelServices.AsNoTracking()
            .CountAsync(s => s.IsActive && s.IsStockManaged && s.StockQuantity <= s.MinStockLevel);

        return new List<PendingWorkItem>
        {
            new()
            {
                Label = "Phòng chờ dọn",
                Count = dirty,
                Icon = "stars",
                Controller = "Housekeeping",
                Action = "Index",
                Hint = dirty > 0 ? "Chưa dọn thì chưa bán lại được." : null
            },
            new()
            {
                Label = "Đơn quá hạn giữ chỗ",
                Count = overdue,
                Icon = "person-x",
                Controller = "Reservations",
                Action = "Index",
                RouteValues = new() { ["tab"] = ReservationsPageViewModel.NoShowTab },
                Hint = overdue > 0 ? "Cần người xác nhận no-show, hệ thống không tự chuyển." : null
            },
            new()
            {
                Label = "Yêu cầu chưa xử lý",
                Count = openRequests,
                Icon = "bell",
                Controller = "Housekeeping",
                Action = "Requests",
                Hint = openRequests > 0 ? "Gồm cả báo hỏng và yêu cầu phục vụ." : null
            },
            new()
            {
                Label = "Hàng tồn dưới định mức",
                Count = lowStock,
                Icon = "box-seam",
                Controller = "Inventory",
                Action = "Index",
                Hint = lowStock > 0 ? "Sắp hết, cần nhập thêm." : null
            }
        };
    }
}
