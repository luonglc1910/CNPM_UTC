using System.Globalization;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
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
}

/// <inheritdoc />
public class ReportService : IReportService
{
    /// <summary>Giới hạn số ngày liệt kê ở báo cáo công suất, tránh vòng lặp quá dài.</summary>
    private const int MaxOccupancyDays = 366;

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
