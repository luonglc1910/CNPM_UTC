using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Ca làm việc thu ngân — FR-F08, FR-F09, BR-10.
///
/// Mọi giao dịch tiền phải gắn một ca đang mở; màn hình khác hỏi <see cref="GetOpenShiftAsync"/>
/// trước khi cho thu/hoàn tiền. Một người chỉ mở được một ca tại một thời điểm (đã chốt thêm ở
/// tầng DB bằng index UX_Shift_OpenPerEmployee). Ca đã đóng là bất biến — không sửa, không xóa.
/// </summary>
public interface IShiftService
{
    Task<CashierShift?> GetOpenShiftAsync(int employeeId);
    Task<ShiftIndexViewModel> BuildIndexAsync(int employeeId, bool isAdmin);
    Task<ServiceResult> OpenAsync(int employeeId, decimal openingCash);
    Task<ServiceResult> CloseAsync(int shiftId, int employeeId, bool isAdmin, CloseShiftForm form);
    Task<ShiftReportViewModel?> BuildReportAsync(int shiftId, int employeeId, bool isAdmin);
}

/// <inheritdoc />
public class ShiftService : IShiftService
{
    private const int RecentShiftCount = 20;

    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly ISettingsReader _settings;

    public ShiftService(HotelDbContext db, IAuditService audit, ISettingsReader settings)
    {
        _db = db;
        _audit = audit;
        _settings = settings;
    }

    public async Task<CashierShift?> GetOpenShiftAsync(int employeeId)
        => await _db.CashierShifts
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Status == ShiftStatus.Open);

    public async Task<ShiftIndexViewModel> BuildIndexAsync(int employeeId, bool isAdmin)
    {
        var openShift = await GetOpenShiftAsync(employeeId);

        var recentQuery = _db.CashierShifts.AsNoTracking();
        if (!isAdmin)
        {
            recentQuery = recentQuery.Where(s => s.EmployeeId == employeeId);
        }

        var recent = await recentQuery
            .OrderByDescending(s => s.OpenedAt).ThenByDescending(s => s.Id)
            .Take(RecentShiftCount)
            .Select(s => new ShiftListItem
            {
                Id = s.Id,
                EmployeeName = s.Employee.FullName,
                OpenedAt = s.OpenedAt,
                ClosedAt = s.ClosedAt,
                Status = s.Status,
                CashDifference = s.CashDifference
            })
            .ToListAsync();

        return new ShiftIndexViewModel
        {
            OpenShift = openShift,
            OpenShiftTotals = openShift is null ? null : await ComputeTotalsAsync(openShift),
            CashDifferenceThreshold = await _settings.GetDecimalAsync(SystemSettingKeys.CashDifferenceThreshold),
            CloseForm = openShift is null ? new CloseShiftForm() : new CloseShiftForm { ShiftId = openShift.Id },
            RecentShifts = recent,
            IsAdmin = isAdmin
        };
    }

    public async Task<ServiceResult> OpenAsync(int employeeId, decimal openingCash)
    {
        if (openingCash < 0)
        {
            return ServiceResult.Fail("Quỹ đầu ca không được âm.", nameof(OpenShiftForm.OpeningCash));
        }

        if (await GetOpenShiftAsync(employeeId) is not null)
        {
            return ServiceResult.Fail("Bạn đang có một ca mở. Đóng ca hiện tại trước khi mở ca mới.");
        }

        _db.CashierShifts.Add(new CashierShift
        {
            EmployeeId = employeeId,
            OpenedAt = DateTime.Now,
            Status = ShiftStatus.Open,
            OpeningCash = openingCash
        });

        _audit.Log("OpenShift", nameof(CashierShift), null,
            newValue: $"Quỹ đầu ca {openingCash:N0} ₫");

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Index UX_Shift_OpenPerEmployee chặn hai ca mở song song của cùng người.
            return ServiceResult.Fail("Bạn đang có một ca mở. Đóng ca hiện tại trước khi mở ca mới.");
        }

        return ServiceResult.Ok(message: $"Đã mở ca với quỹ đầu ca {openingCash:N0} ₫.");
    }

    public async Task<ServiceResult> CloseAsync(int shiftId, int employeeId, bool isAdmin, CloseShiftForm form)
    {
        var shift = await _db.CashierShifts.FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null)
        {
            return ServiceResult.Fail("Không tìm thấy ca làm việc.");
        }

        if (!isAdmin && shift.EmployeeId != employeeId)
        {
            return ServiceResult.Fail("Bạn chỉ được đóng ca của chính mình.");
        }

        if (shift.Status == ShiftStatus.Closed)
        {
            return ServiceResult.Fail("Ca này đã đóng, không thể đóng lại.");
        }

        if (form.CountedCash < 0)
        {
            return ServiceResult.Fail("Số tiền đếm không được âm.", nameof(CloseShiftForm.CountedCash));
        }

        var totals = await ComputeTotalsAsync(shift);
        var difference = form.CountedCash - totals.ExpectedCash;

        if (difference != 0 && string.IsNullOrWhiteSpace(form.DifferenceReason))
        {
            return ServiceResult.Fail(
                $"Chênh lệch {difference:N0} ₫ so với sổ sách — bắt buộc nhập lý do.",
                nameof(CloseShiftForm.DifferenceReason));
        }

        shift.ClosedAt = DateTime.Now;
        shift.CountedCash = form.CountedCash;
        shift.ExpectedCash = totals.ExpectedCash;
        shift.CashDifference = difference;
        shift.DifferenceReason = difference != 0 ? form.DifferenceReason!.Trim() : null;
        shift.Status = ShiftStatus.Closed;

        var threshold = await _settings.GetDecimalAsync(SystemSettingKeys.CashDifferenceThreshold);
        var overThreshold = Math.Abs(difference) > threshold;

        _audit.Log(overThreshold ? "CloseShiftLargeDifference" : "CloseShift",
            nameof(CashierShift), shift.Id.ToString(),
            reason: shift.DifferenceReason,
            oldValue: $"Sổ sách {totals.ExpectedCash:N0} ₫",
            newValue: $"Đếm {form.CountedCash:N0} ₫ (chênh {difference:N0} ₫)");

        await _db.SaveChangesAsync();

        var warning = overThreshold
            ? $"Chênh lệch {difference:N0} ₫ vượt ngưỡng {threshold:N0} ₫ — đã ghi nhật ký mức cao."
            : null;

        return ServiceResult.Ok(warning, $"Đã đóng ca. Chênh lệch tiền mặt: {difference:N0} ₫.");
    }

    public async Task<ShiftReportViewModel?> BuildReportAsync(int shiftId, int employeeId, bool isAdmin)
    {
        var shift = await _db.CashierShifts.AsNoTracking()
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift is null || (!isAdmin && shift.EmployeeId != employeeId))
        {
            return null;
        }

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.CashierShiftId == shiftId)
            .OrderBy(i => i.InvoiceNo)
            .Select(i => new ShiftInvoiceItem
            {
                InvoiceId = i.Id,
                InvoiceNo = i.InvoiceNo,
                RoomNumber = i.Folio.Stay.Room.RoomNumber,
                GuestName = i.Folio.Stay.PrimaryGuest.FullName,
                TotalAmount = i.TotalAmount,
                Status = i.Status
            })
            .ToListAsync();

        return new ShiftReportViewModel
        {
            Shift = shift,
            EmployeeName = shift.Employee.FullName,
            Totals = await ComputeTotalsAsync(shift),
            Invoices = invoices
        };
    }

    /// <summary>Tổng hợp thu/chi và doanh thu của một ca từ Payment và Invoice thuộc ca đó.</summary>
    private async Task<ShiftTotals> ComputeTotalsAsync(CashierShift shift)
    {
        var payments = _db.Payments.AsNoTracking().Where(p => p.CashierShiftId == shift.Id);

        var cashIn = await payments
            .Where(p => p.Method == PaymentMethod.Cash && p.Amount > 0)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var cashRefund = await payments
            .Where(p => p.Method == PaymentMethod.Cash && p.Amount < 0)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var bankIn = await payments
            .Where(p => p.Method == PaymentMethod.BankTransfer)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var cardIn = await payments
            .Where(p => p.Method == PaymentMethod.Card)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var refundTotal = await payments
            .Where(p => p.Amount < 0)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var cancellationRevenue = await payments
            .Where(p => p.Type == PaymentType.CancellationFee)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var paymentCount = await payments.CountAsync();

        var invoices = _db.Invoices.AsNoTracking()
            .Where(i => i.CashierShiftId == shift.Id && i.Status != InvoiceStatus.Void);

        var invoiceCount = await invoices.CountAsync();
        var roomRevenue = await invoices.SumAsync(i => (decimal?)i.RoomCharge) ?? 0m;
        var serviceRevenue = await invoices.SumAsync(i => (decimal?)i.ServiceCharge) ?? 0m;
        var surchargeRevenue = await invoices.SumAsync(i => (decimal?)i.SurchargeAmount) ?? 0m;

        return new ShiftTotals
        {
            OpeningCash = shift.OpeningCash,
            CashIn = cashIn,
            CashRefund = cashRefund,
            ExpectedCash = shift.OpeningCash + cashIn + cashRefund,
            BankTransferIn = bankIn,
            CardIn = cardIn,
            PaymentCount = paymentCount,
            InvoiceCount = invoiceCount,
            RoomRevenue = roomRevenue,
            ServiceRevenue = serviceRevenue,
            SurchargeRevenue = surchargeRevenue,
            CancellationRevenue = cancellationRevenue,
            RefundTotal = refundTotal
        };
    }
}
