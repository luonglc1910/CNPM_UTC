using System.Globalization;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Báo cáo quản trị — nhóm G (SCR-G01 doanh thu, SCR-G02 công suất phòng), FR-G01…G03.
///
/// Chỉ đọc dữ liệu (AsNoTracking). Doanh thu lấy từ hóa đơn đã chốt (loại Void) cộng phí hủy;
/// công suất tính từ các lượt lưu trú giao với từng đêm trong khoảng, mẫu số là số phòng còn
/// khai thác được (loại phòng OutOfService — FR-G03).
/// </summary>
public interface IReportService
{
    Task<RevenueReportViewModel> BuildRevenueAsync(RevenueReportViewModel filter);
    Task<OccupancyReportViewModel> BuildOccupancyAsync(OccupancyReportViewModel filter);
    Task<ServicesReportViewModel> BuildServicesAsync(ServicesReportViewModel filter);
    Task<StaffReportViewModel> BuildStaffAsync(StaffReportViewModel filter);
    Task<AuditLogReportViewModel> BuildAuditLogAsync(AuditLogReportViewModel filter, int page);
}

/// <inheritdoc />
public class ReportService : IReportService
{
    /// <summary>Giới hạn số ngày liệt kê ở báo cáo công suất, tránh vòng lặp quá dài.</summary>
    private const int MaxOccupancyDays = 366;

    /// <summary>
    /// Những hành động ghi lại việc người dùng bấm tiếp dù hệ thống đã cảnh báo — SCR-G04 tab 2.
    /// Chỉ nhật ký mới biết được số này: bản thân bản ghi nghiệp vụ không lưu dấu vết cảnh báo.
    /// </summary>
    public static readonly string[] OverrideActions =
    {
        AuditActions.OverrideBlacklistWarning,
        AuditActions.OverrideNegativeStock
    };

    private readonly HotelDbContext _db;

    public ReportService(HotelDbContext db)
    {
        _db = db;
    }

    // ---------- SCR-G01 ----------

    public async Task<RevenueReportViewModel> BuildRevenueAsync(RevenueReportViewModel filter)
    {
        NormalizeRange(filter, defaultFrom: FirstDayOfMonth(), defaultTo: DateTime.Now.Date);

        var from = filter.From.Date;
        var toExclusive = filter.To.Date.AddDays(1);

        var invoiceRows = await _db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Settled && i.IssuedAt >= from && i.IssuedAt < toExclusive)
            .Select(i => new
            {
                i.IssuedAt,
                i.RoomCharge,
                i.ServiceCharge,
                i.SurchargeAmount,
                i.DiscountAmount,
                i.TaxAmount,
                i.TotalAmount
            })
            .ToListAsync();

        var cancellationRows = await _db.Reservations.AsNoTracking()
            .Where(r => (r.Status == ReservationStatus.Cancelled || r.Status == ReservationStatus.NoShow)
                && r.CancellationFee > 0
                && r.CancelledAt != null
                && r.CancelledAt >= from && r.CancelledAt < toExclusive)
            .Select(r => new { At = r.CancelledAt!.Value, r.CancellationFee })
            .ToListAsync();

        var buckets = new Dictionary<DateTime, RevenueRow>();

        RevenueRow Bucket(DateTime date)
        {
            var key = PeriodKey(date, filter.Period);
            if (!buckets.TryGetValue(key, out var row))
            {
                row = new RevenueRow { SortKey = key, Label = PeriodLabel(key, filter.Period) };
                buckets[key] = row;
            }

            return row;
        }

        foreach (var inv in invoiceRows)
        {
            var row = Bucket(inv.IssuedAt);
            row.Room += inv.RoomCharge;
            row.Service += inv.ServiceCharge;
            row.Surcharge += inv.SurchargeAmount;
            row.Discount += inv.DiscountAmount;
            row.Vat += inv.TaxAmount;
        }

        foreach (var c in cancellationRows)
        {
            Bucket(c.At).Cancellation += c.CancellationFee;
        }

        var rows = buckets.Values.OrderBy(r => r.SortKey).ToList();

        var total = new RevenueRow
        {
            Label = "Tổng cộng",
            Room = rows.Sum(r => r.Room),
            Service = rows.Sum(r => r.Service),
            Surcharge = rows.Sum(r => r.Surcharge),
            Discount = rows.Sum(r => r.Discount),
            Cancellation = rows.Sum(r => r.Cancellation),
            Vat = rows.Sum(r => r.Vat)
        };

        filter.Rows = rows;
        filter.Total = total;
        filter.GrossSettled = invoiceRows.Sum(i => i.TotalAmount);
        filter.InvoiceCount = invoiceRows.Count;
        return filter;
    }

    // ---------- SCR-G02 ----------

    public async Task<OccupancyReportViewModel> BuildOccupancyAsync(OccupancyReportViewModel filter)
    {
        NormalizeRange(filter, defaultFrom: DateTime.Now.Date.AddDays(-13), defaultTo: DateTime.Now.Date);

        var from = filter.From.Date;
        var to = filter.To.Date;

        var totalDays = (to - from).Days + 1;
        var truncated = totalDays > MaxOccupancyDays;
        if (truncated)
        {
            totalDays = MaxOccupancyDays;
            to = from.AddDays(totalDays - 1);
        }

        var usableRooms = await _db.Rooms.AsNoTracking()
            .CountAsync(r => r.IsActive && r.Status != RoomStatus.OutOfService);

        var rangeEndExclusive = to.AddDays(1);

        var stays = await _db.Stays.AsNoTracking()
            .Where(s => s.ActualCheckIn < rangeEndExclusive
                && (s.ActualCheckOut ?? s.ExpectedCheckOut) > from)
            .Select(s => new
            {
                s.RoomId,
                Start = s.ActualCheckIn,
                End = s.ActualCheckOut ?? s.ExpectedCheckOut
            })
            .ToListAsync();

        var rows = new List<OccupancyRow>(totalDays);
        var sold = 0;

        for (var i = 0; i < totalDays; i++)
        {
            var day = from.AddDays(i);

            // Một phòng có khách trong đêm "day" khi nhận trước/đúng ngày đó và trả sau ngày đó.
            var occupied = stays
                .Where(s => s.Start.Date <= day && s.End.Date > day)
                .Select(s => s.RoomId)
                .Distinct()
                .Count();

            sold += occupied;
            rows.Add(new OccupancyRow { Date = day, OccupiedRooms = occupied, UsableRooms = usableRooms });
        }

        filter.UsableRooms = usableRooms;
        filter.Rows = rows;
        filter.SoldRoomNights = sold;
        filter.CapacityRoomNights = usableRooms * totalDays;
        filter.Truncated = truncated;
        return filter;
    }

    // ---------- SCR-G03 ----------

    public async Task<ServicesReportViewModel> BuildServicesAsync(ServicesReportViewModel filter)
    {
        NormalizeRange(filter, defaultFrom: FirstDayOfMonth(), defaultTo: DateTime.Now.Date);

        var from = filter.From.Date;
        var toExclusive = filter.To.Date.AddDays(1);

        // Tab 1 — dịch vụ bán chạy. Chỉ tính dòng folio loại Service chưa bị hủy và có gắn dịch vụ:
        // dòng đã void không còn là doanh thu, dòng không gắn dịch vụ là phụ thu nhập tay.
        var sales = await _db.FolioItems.AsNoTracking()
            .Where(f => f.ItemType == FolioItemType.Service
                && !f.IsVoided
                && f.HotelServiceId != null
                && f.ChargedAt >= from && f.ChargedAt < toExclusive)
            .GroupBy(f => new { f.HotelServiceId, f.HotelService!.Code, f.HotelService.Name, f.HotelService.Category })
            .Select(g => new ServiceSalesRow
            {
                Code = g.Key.Code,
                Name = g.Key.Name,
                Category = g.Key.Category,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        var totalRevenue = sales.Sum(s => s.Revenue);

        foreach (var row in sales)
        {
            row.Share = totalRevenue == 0 ? 0 : (double)(row.Revenue / totalRevenue) * 100;
        }

        var byCategory = sales
            .GroupBy(s => s.Category)
            .Select(g => new CategorySalesRow
            {
                Category = g.Key,
                Revenue = g.Sum(x => x.Revenue),
                Share = totalRevenue == 0 ? 0 : (double)(g.Sum(x => x.Revenue) / totalRevenue) * 100
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // Tab 2 — đối chiếu kho. Quantity trong InventoryTransaction là số chênh có dấu
        // (nhập dương, bán âm, điều chỉnh +/−), nên tồn đầu cộng tổng chênh phải ra tồn cuối.
        var managed = await _db.HotelServices.AsNoTracking()
            .Where(s => s.IsStockManaged)
            .Select(s => new { s.Id, s.Code, s.Name, s.Unit, s.MinStockLevel, s.StockQuantity })
            .OrderBy(s => s.Code)
            .ToListAsync();

        var ids = managed.Select(s => s.Id).ToList();

        // Tồn đầu kỳ = tồn sau giao dịch cuối cùng trước mốc bắt đầu.
        var openings = await LastStockAfterAsync(ids, from);

        // Chỉ đối chiếu được với tồn hiện tại khi kỳ kéo tới hôm nay; kỳ đã khép trong quá khứ
        // thì tồn hiện tại đã chạy tiếp, lệch là đương nhiên chứ không phải dấu hiệu ghi sai.
        var comparable = filter.To.Date >= DateTime.Now.Date;

        var movements = await _db.InventoryTransactions.AsNoTracking()
            .Where(t => ids.Contains(t.HotelServiceId)
                && t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .GroupBy(t => new { t.HotelServiceId, t.Type })
            .Select(g => new { g.Key.HotelServiceId, g.Key.Type, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var stock = new List<StockReconciliationRow>(managed.Count);

        foreach (var svc in managed)
        {
            var mine = movements.Where(m => m.HotelServiceId == svc.Id).ToList();
            int Sum(InventoryTransactionType type) => mine.Where(m => m.Type == type).Sum(m => m.Quantity);

            var opening = openings.GetValueOrDefault(svc.Id);

            stock.Add(new StockReconciliationRow
            {
                Code = svc.Code,
                Name = svc.Name,
                Unit = svc.Unit,
                MinStockLevel = svc.MinStockLevel,
                Opening = opening,
                Received = Sum(InventoryTransactionType.Receive),
                // Bán lưu số âm; đổi dấu để bảng đọc ra "đã bán bao nhiêu".
                Sold = -Sum(InventoryTransactionType.Sale),
                Adjusted = Sum(InventoryTransactionType.Adjust),
                Returned = Sum(InventoryTransactionType.Return),
                BookStock = svc.StockQuantity,
                ComparableToBookStock = comparable
            });
        }

        filter.Sales = sales.OrderByDescending(s => s.Revenue).ToList();
        filter.ByCategory = byCategory;
        filter.Stock = stock;
        filter.TotalRevenue = totalRevenue;
        filter.TotalQuantity = sales.Sum(s => s.Quantity);
        return filter;
    }

    /// <summary>
    /// Tồn sau giao dịch gần nhất của từng dịch vụ, tính tới một mốc thời gian.
    ///
    /// Viết dưới dạng truy vấn con tương quan từ phía HotelServices thay vì GroupBy rồi
    /// OrderByDescending().First(): dạng sau EF Core thường từ chối dịch và ném lỗi lúc chạy.
    /// </summary>
    private async Task<Dictionary<int, int>> LastStockAfterAsync(List<int> serviceIds, DateTime before)
    {
        var rows = await _db.HotelServices.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                StockAfter = s.InventoryTransactions
                    .Where(t => t.CreatedAt < before)
                    .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
                    .Select(t => (int?)t.StockAfter)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return rows.ToDictionary(x => x.Id, x => x.StockAfter ?? 0);
    }

    // ---------- SCR-G04 ----------

    public async Task<StaffReportViewModel> BuildStaffAsync(StaffReportViewModel filter)
    {
        NormalizeRange(filter, defaultFrom: FirstDayOfMonth(), defaultTo: DateTime.Now.Date);

        var from = filter.From.Date;
        var toExclusive = filter.To.Date.AddDays(1);

        filter.EmployeeOptions = await _db.Employees.AsNoTracking()
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeOption { Id = e.Id, FullName = e.FullName, Role = e.Role })
            .ToListAsync();

        // Tab 1 — theo ca thu ngân. Lọc theo giờ MỞ ca: một ca mở cuối kỳ và đóng sang kỳ sau
        // vẫn thuộc về kỳ đã mở, nếu không tiền của ca đó rơi ra ngoài mọi báo cáo.
        var shiftQuery = _db.CashierShifts.AsNoTracking()
            .Where(s => s.OpenedAt >= from && s.OpenedAt < toExclusive);

        if (filter.EmployeeId is int empId)
        {
            shiftQuery = shiftQuery.Where(s => s.EmployeeId == empId);
        }

        if (filter.Role is EmployeeRole role)
        {
            shiftQuery = shiftQuery.Where(s => s.Employee.Role == role);
        }

        var shifts = await shiftQuery
            .OrderByDescending(s => s.OpenedAt)
            .Select(s => new ShiftSummaryRow
            {
                ShiftId = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee.FullName,
                OpenedAt = s.OpenedAt,
                ClosedAt = s.ClosedAt,
                Status = s.Status,
                CashDifference = s.CashDifference,
                DifferenceReason = s.DifferenceReason,
                Cash = s.Payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => (decimal?)p.Amount) ?? 0m,
                BankTransfer = s.Payments.Where(p => p.Method == PaymentMethod.BankTransfer).Sum(p => (decimal?)p.Amount) ?? 0m,
                Card = s.Payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();

        // Cộng dồn chênh lệch theo người. Cộng cả trị tuyệt đối vì hai ca lệch +1tr và −1tr
        // triệt tiêu thành 0 sẽ che mất đúng người cần để ý.
        var cashDifferences = shifts
            .GroupBy(s => s.EmployeeName)
            .Select(g => new StaffCashDifferenceRow
            {
                EmployeeName = g.Key,
                ShiftCount = g.Count(),
                DifferenceCount = g.Count(x => x.HasDifference),
                TotalDifference = g.Sum(x => x.CashDifference ?? 0m),
                TotalAbsoluteDifference = g.Sum(x => Math.Abs(x.CashDifference ?? 0m))
            })
            .OrderByDescending(r => r.TotalAbsoluteDifference)
            .ToList();

        // Tab 2 — thao tác của lễ tân. Quy trách nhiệm bằng CreatedBy/CancelledBy do
        // HotelDbContext.SaveChanges tự gán, trừ số lần bỏ qua cảnh báo vốn chỉ có ở nhật ký.
        var employees = filter.EmployeeOptions
            .Where(e => filter.EmployeeId is null || e.Id == filter.EmployeeId)
            .Where(e => filter.Role is null || e.Role == filter.Role)
            .ToList();

        var empIds = employees.Select(e => e.Id).ToList();

        var checkIns = await _db.Stays.AsNoTracking()
            .Where(s => s.CreatedBy != null && empIds.Contains(s.CreatedBy.Value)
                && s.ActualCheckIn >= from && s.ActualCheckIn < toExclusive)
            .GroupBy(s => s.CreatedBy!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        var checkOuts = await _db.Stays.AsNoTracking()
            .Where(s => s.UpdatedBy != null && empIds.Contains(s.UpdatedBy.Value)
                && s.ActualCheckOut != null
                && s.ActualCheckOut >= from && s.ActualCheckOut < toExclusive)
            .GroupBy(s => s.UpdatedBy!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        var created = await _db.Reservations.AsNoTracking()
            .Where(r => r.CreatedBy != null && empIds.Contains(r.CreatedBy.Value)
                && r.CreatedAt >= from && r.CreatedAt < toExclusive)
            .GroupBy(r => r.CreatedBy!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        var cancelled = await _db.Reservations.AsNoTracking()
            .Where(r => r.CancelledBy != null && empIds.Contains(r.CancelledBy.Value)
                && r.CancelledAt >= from && r.CancelledAt < toExclusive)
            .GroupBy(r => r.CancelledBy!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        var discounts = await _db.FolioItems.AsNoTracking()
            .Where(f => f.ItemType == FolioItemType.Discount
                && !f.IsVoided
                && f.CreatedBy != null && empIds.Contains(f.CreatedBy.Value)
                && f.ChargedAt >= from && f.ChargedAt < toExclusive)
            .GroupBy(f => f.CreatedBy!.Value)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(x => Math.Abs(x.Amount)) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Total);

        var overrides = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.UserId != null && empIds.Contains(a.UserId.Value)
                && a.CreatedAt >= from && a.CreatedAt < toExclusive
                && OverrideActions.Contains(a.Action))
            .GroupBy(a => a.UserId!.Value)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count);

        filter.Shifts = shifts;
        filter.CashDifferences = cashDifferences;
        filter.Activity = employees
            .Select(e => new ReceptionistActivityRow
            {
                EmployeeId = e.Id,
                EmployeeName = e.FullName,
                Role = e.Role,
                CheckIns = checkIns.GetValueOrDefault(e.Id),
                CheckOuts = checkOuts.GetValueOrDefault(e.Id),
                ReservationsCreated = created.GetValueOrDefault(e.Id),
                ReservationsCancelled = cancelled.GetValueOrDefault(e.Id),
                TotalDiscount = discounts.GetValueOrDefault(e.Id),
                WarningOverrides = overrides.GetValueOrDefault(e.Id)
            })
            .Where(r => r.CheckIns + r.CheckOuts + r.ReservationsCreated
                + r.ReservationsCancelled + r.WarningOverrides > 0 || r.TotalDiscount > 0)
            .OrderByDescending(r => r.CheckIns + r.CheckOuts)
            .ToList();

        return filter;
    }

    // ---------- SCR-G05 ----------

    public async Task<AuditLogReportViewModel> BuildAuditLogAsync(AuditLogReportViewModel filter, int page)
    {
        NormalizeRange(filter, defaultFrom: DateTime.Now.Date.AddDays(-6), defaultTo: DateTime.Now.Date);

        var from = filter.From.Date;
        var toExclusive = filter.To.Date.AddDays(1);

        var query = _db.AuditLogs.AsNoTracking()
            .Where(a => a.CreatedAt >= from && a.CreatedAt < toExclusive);

        if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            var user = filter.UserName.Trim();
            query = query.Where(a => a.UserName == user);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
        {
            var action = filter.ActionType.Trim();
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            // Ô tìm chung cho mã đơn / số hóa đơn / số phòng: đối tượng được ghi bằng mã số,
            // còn mã nghiệp vụ dạng chuỗi nằm trong giá trị cũ/mới.
            var term = filter.EntityId.Trim();
            query = query.Where(a =>
                (a.EntityId != null && a.EntityId.Contains(term))
                || (a.NewValue != null && a.NewValue.Contains(term))
                || (a.OldValue != null && a.OldValue.Contains(term)));
        }

        var projected = query
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => new AuditLogRow
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                UserName = a.UserName,
                UserRole = a.UserRole,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                Reason = a.Reason,
                IpAddress = a.IpAddress
            });

        filter.Items = await PagedList<AuditLogRow>.CreateAsync(projected, page);

        // Ô chọn chỉ liệt kê giá trị thực sự có trong khoảng đang xem, tránh danh sách dài
        // toàn những hành động chưa từng xảy ra.
        var scope = _db.AuditLogs.AsNoTracking()
            .Where(a => a.CreatedAt >= from && a.CreatedAt < toExclusive);

        filter.ActionOptions = await scope.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
        filter.UserOptions = await scope.Select(a => a.UserName).Distinct().OrderBy(u => u).ToListAsync();

        return filter;
    }

    // ---------- Helpers ----------

    private static DateTime FirstDayOfMonth()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, 1);
    }

    private static void NormalizeRange(IDateRangeReport report, DateTime defaultFrom, DateTime defaultTo)
    {
        if (report.From == default)
        {
            report.From = defaultFrom;
        }

        if (report.To == default)
        {
            report.To = defaultTo;
        }

        if (report.To < report.From)
        {
            report.To = report.From;
        }
    }

    private static DateTime PeriodKey(DateTime date, RevenuePeriod period) => period switch
    {
        RevenuePeriod.Month => new DateTime(date.Year, date.Month, 1),
        RevenuePeriod.Year => new DateTime(date.Year, 1, 1),
        _ => date.Date
    };

    private static string PeriodLabel(DateTime key, RevenuePeriod period) => period switch
    {
        RevenuePeriod.Month => key.ToString("MM/yyyy", CultureInfo.InvariantCulture),
        RevenuePeriod.Year => key.ToString("yyyy", CultureInfo.InvariantCulture),
        _ => key.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
    };
}
