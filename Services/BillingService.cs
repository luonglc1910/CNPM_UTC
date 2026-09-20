using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Thu ngân — nhóm F (SCR-F01…F07) + kiểm minibar (E02), FR-F01…F07, BR-04, BR-08, BR-10, BR-12.
///
/// Công thức folio theo BR-04 dùng <see cref="IPricingService.ComputeFolioTotal"/>: VAT áp trên
/// (phòng + dịch vụ + phụ thu − giảm giá), KHÔNG trừ cọc; cọc chỉ đối trừ để ra số dư phải thu.
/// Mọi thao tác tiền chạy trong <see cref="ITransactionRunner"/> và cần ca mở (<see cref="IShiftService"/>).
/// </summary>
public interface IBillingService
{
    Task<BillingFolioSummary?> ComputeFolioSummaryAsync(int stayId);

    Task<BillingIndexViewModel> BuildIndexAsync(BillingIndexViewModel filter, int page);
    Task<FolioDetailViewModel?> GetFolioAsync(int stayId);

    Task<AddChargeViewModel?> BuildAddChargeAsync(int stayId);
    Task<ServiceResult> AddChargeAsync(AddChargeViewModel form, int employeeId);
    Task<ServiceResult> VoidLineAsync(int folioItemId, string reason, int employeeId);

    Task<ServiceResult> LockFolioAsync(int stayId, int employeeId);
    Task<ServiceResult> UnlockFolioAsync(int stayId, int employeeId);

    Task<DiscountViewModel?> BuildDiscountAsync(int stayId, bool isAdmin);
    Task<ServiceResult> ApplyDiscountAsync(DiscountViewModel form, int employeeId, bool isAdmin);

    Task<PaymentViewModel?> BuildPaymentAsync(int stayId, int employeeId, bool isAdmin);
    Task<(ServiceResult Result, int InvoiceId)> PayAsync(PaymentViewModel form, int employeeId, bool isAdmin);

    Task<InvoiceViewModel?> BuildInvoiceAsync(int invoiceId);
    Task<VoidInvoiceViewModel?> BuildVoidAsync(int invoiceId, int employeeId);
    Task<ServiceResult> VoidInvoiceAsync(VoidInvoiceViewModel form, int employeeId);

    Task<MinibarViewModel?> BuildMinibarAsync(int stayId);
    Task<ServiceResult> SaveMinibarAsync(MinibarViewModel form, int employeeId);
    /// <summary>Trạng thái lượt lưu trú, null nếu không có. Chỉ dùng để giải thích lỗi.</summary>
    Task<StayStatus?> GetStayStatusAsync(int stayId);

    /// <summary>Trạng thái hóa đơn, null nếu không có. Chỉ dùng để giải thích lỗi.</summary>
    Task<InvoiceStatus?> GetInvoiceStatusAsync(int invoiceId);

}

/// <inheritdoc />
public class BillingService : IBillingService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;
    private readonly IPricingService _pricing;
    private readonly INumberSequenceService _numbers;
    private readonly ITransactionRunner _tx;
    private readonly IShiftService _shifts;
    private readonly ISettingsReader _settings;
    private readonly IInventoryService _inventory;

    public BillingService(
        HotelDbContext db, IAuditService audit, IPricingService pricing, INumberSequenceService numbers,
        ITransactionRunner tx, IShiftService shifts, ISettingsReader settings, IInventoryService inventory)
    {
        _db = db;
        _audit = audit;
        _pricing = pricing;
        _numbers = numbers;
        _tx = tx;
        _shifts = shifts;
        _settings = settings;
        _inventory = inventory;
    }

    // ---------- Tổng hợp folio (dùng chung) ----------

    public async Task<BillingFolioSummary?> ComputeFolioSummaryAsync(int stayId)
    {
        var folio = await _db.Folios.AsNoTracking()
            .Include(f => f.Items)
            .Include(f => f.Stay)
            .FirstOrDefaultAsync(f => f.StayId == stayId);

        if (folio is null)
        {
            return null;
        }

        var deposit = await HeldDepositAsync(folio.Stay);
        var settings = await _settings.GetPricingSettingsAsync();
        return BuildSummary(folio, deposit, settings);
    }

    private BillingFolioSummary BuildSummary(Folio folio, decimal depositHeld, PricingSettings settings)
    {
        var items = folio.Items.Where(i => !i.IsVoided).ToList();
        var room = items.Where(i => i.ItemType == FolioItemType.Room).Sum(i => i.Amount);
        var service = items.Where(i => i.ItemType == FolioItemType.Service).Sum(i => i.Amount);
        var surcharge = items.Where(i => i.ItemType == FolioItemType.Surcharge).Sum(i => i.Amount);
        var discount = -items.Where(i => i.ItemType == FolioItemType.Discount).Sum(i => i.Amount);

        var total = _pricing.ComputeFolioTotal(room, service, surcharge, discount, depositHeld, settings);

        return new BillingFolioSummary
        {
            FolioId = folio.Id,
            FolioCode = folio.Code,
            StayId = folio.StayId,
            IsLocked = folio.IsLocked,
            RoomCharge = total.RoomCharge,
            ServiceCharge = total.ServiceCharge,
            SurchargeAmount = total.SurchargeAmount,
            DiscountAmount = total.DiscountAmount,
            SubTotal = total.SubTotal,
            TaxRate = total.TaxRate,
            TaxAmount = total.TaxAmount,
            Total = total.Total,
            DepositApplied = total.DepositApplied,
            BalanceDue = total.BalanceDue
        };
    }

    private async Task<decimal> HeldDepositAsync(Stay stay)
    {
        var byStay = await _db.Deposits.Where(d => d.StayId == stay.Id && d.Status == DepositStatus.Held)
            .SumAsync(d => (decimal?)d.Amount) ?? 0m;

        var byReservation = stay.ReservationId is null ? 0m
            : await _db.Deposits.Where(d => d.ReservationId == stay.ReservationId && d.Status == DepositStatus.Held)
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

        return byStay + byReservation;
    }

    // ---------- SCR-F01 ----------

    public async Task<BillingIndexViewModel> BuildIndexAsync(BillingIndexViewModel filter, int page)
    {
        var settings = await _settings.GetPricingSettingsAsync();

        if (filter.IsInvoiceTab)
        {
            var invQuery = _db.Invoices.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var k = filter.Keyword.Trim();
                invQuery = invQuery.Where(i => i.InvoiceNo.Contains(k)
                    || i.Folio.Stay.Room.RoomNumber.Contains(k)
                    || i.Folio.Stay.PrimaryGuest.FullName.Contains(k));
            }

            var invProjected = invQuery
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => new InvoiceListItem
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    IssuedAt = i.IssuedAt,
                    RoomNumber = i.Folio.Stay.Room.RoomNumber,
                    GuestName = i.Folio.Stay.PrimaryGuest.FullName,
                    TotalAmount = i.TotalAmount,
                    TaxAmount = i.TaxAmount,
                    Status = i.Status
                });

            filter.Invoices = await PagedList<InvoiceListItem>.CreateAsync(invProjected, page);
            return filter;
        }

        // Tab folio đang mở: nạp folio của các lượt đang lưu trú rồi tính tổng trong bộ nhớ
        // (mỗi lượt là một folio nhỏ, số lượng đang mở không lớn).
        var openStays = await _db.Folios.AsNoTracking()
            .Where(f => f.Stay.Status == StayStatus.CheckedIn)
            .Include(f => f.Items)
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .Include(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .ToListAsync();

        var keyword = filter.Keyword?.Trim();
        var items = new List<OpenFolioListItem>();
        foreach (var f in openStays)
        {
            if (!string.IsNullOrWhiteSpace(keyword)
                && !f.Stay.Room.RoomNumber.Contains(keyword)
                && !f.Stay.PrimaryGuest.FullName.Contains(keyword)
                && !f.Code.Contains(keyword))
            {
                continue;
            }

            var deposit = await HeldDepositAsync(f.Stay);
            var summary = BuildSummary(f, deposit, settings);
            items.Add(new OpenFolioListItem
            {
                StayId = f.StayId,
                FolioCode = f.Code,
                RoomNumber = f.Stay.Room.RoomNumber,
                GuestName = f.Stay.PrimaryGuest.FullName,
                ActualCheckIn = f.Stay.ActualCheckIn,
                ExpectedCheckOut = f.Stay.ExpectedCheckOut,
                SubTotal = summary.SubTotal,
                DepositApplied = summary.DepositApplied,
                BalanceDue = summary.BalanceDue,
                IsLocked = f.IsLocked
            });
        }

        var ordered = items.OrderBy(i => i.RoomNumber).ToList();
        var total = ordered.Count;
        var paged = ordered.Skip((Math.Max(1, page) - 1) * PagedList<OpenFolioListItem>.DefaultPageSize)
            .Take(PagedList<OpenFolioListItem>.DefaultPageSize).ToList();

        filter.OpenFolios = new PagedList<OpenFolioListItem>
        {
            Items = paged,
            Page = Math.Max(1, page),
            PageSize = PagedList<OpenFolioListItem>.DefaultPageSize,
            TotalItems = total
        };
        return filter;
    }

    // ---------- SCR-F02 ----------

    public async Task<FolioDetailViewModel?> GetFolioAsync(int stayId)
    {
        var folio = await _db.Folios.AsNoTracking()
            .Include(f => f.Items)
            .Include(f => f.Stay).ThenInclude(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .Include(f => f.Invoice)
            .FirstOrDefaultAsync(f => f.StayId == stayId);

        if (folio is null)
        {
            return null;
        }

        var deposit = await HeldDepositAsync(folio.Stay);
        var settings = await _settings.GetPricingSettingsAsync();

        return new FolioDetailViewModel
        {
            StayId = stayId,
            RoomNumber = folio.Stay.Room.RoomNumber,
            RoomTypeName = folio.Stay.Room.RoomType.Name,
            GuestName = folio.Stay.PrimaryGuest.FullName,
            ActualCheckIn = folio.Stay.ActualCheckIn,
            ExpectedCheckOut = folio.Stay.ExpectedCheckOut,
            Nights = folio.Stay.Nights,
            StayStatus = folio.Stay.Status,
            InvoiceId = folio.Invoice?.Id,
            Summary = BuildSummary(folio, deposit, settings),
            Lines = folio.Items
                .OrderBy(i => i.ChargedAt).ThenBy(i => i.Id)
                .Select(i => new FolioLineView
                {
                    Id = i.Id,
                    ChargedAt = i.ChargedAt,
                    ItemType = i.ItemType,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    IsVoided = i.IsVoided
                }).ToList()
        };
    }

    // ---------- SCR-F03 ----------

    public async Task<AddChargeViewModel?> BuildAddChargeAsync(int stayId)
    {
        var stay = await _db.Stays.AsNoTracking().Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay is null)
        {
            return null;
        }

        return new AddChargeViewModel
        {
            StayId = stayId,
            RoomNumber = stay.Room.RoomNumber,
            ServiceOptions = await ServiceOptionsAsync()
        };
    }

    public async Task<ServiceResult> AddChargeAsync(AddChargeViewModel form, int employeeId)
    {
        var folio = await _db.Folios.Include(f => f.Stay).FirstOrDefaultAsync(f => f.StayId == form.StayId);
        if (folio is null)
        {
            return ServiceResult.Fail("Không tìm thấy folio.");
        }

        if (folio.IsLocked || folio.Stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Folio đã khóa hoặc lượt lưu trú đã đóng, không thêm chi phí được.");
        }

        if (form.Quantity <= 0)
        {
            return ServiceResult.Fail("Số lượng phải lớn hơn 0.", nameof(form.Quantity));
        }

        HotelService? service = null;
        var description = form.Description?.Trim() ?? string.Empty;
        var unitPrice = form.UnitPrice;

        if (form.ItemType == FolioItemType.Service)
        {
            if (form.HotelServiceId is null)
            {
                return ServiceResult.Fail("Vui lòng chọn dịch vụ.", nameof(form.HotelServiceId));
            }

            service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == form.HotelServiceId);
            if (service is null)
            {
                return ServiceResult.Fail("Không tìm thấy dịch vụ.");
            }

            unitPrice = service.UnitPrice;
            description = service.Name;

            if (service.IsStockManaged && service.StockQuantity < form.Quantity)
            {
                var allowGlobal = await _settings.GetBoolAsync(SystemSettingKeys.AllowSellWhenOutOfStock);
                if (!(service.AllowNegativeStock && allowGlobal))
                {
                    return ServiceResult.Fail(
                        $"Dịch vụ {service.Code} chỉ còn {service.StockQuantity} {service.Unit}, không đủ bán {form.Quantity}.");
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(description))
        {
            return ServiceResult.Fail("Vui lòng nhập nội dung phụ thu.", nameof(form.Description));
        }

        await _tx.ExecuteAsync(async () =>
        {
            var item = new FolioItem
            {
                FolioId = folio.Id,
                ItemType = form.ItemType,
                HotelServiceId = service?.Id,
                Description = description,
                Quantity = form.Quantity,
                UnitPrice = unitPrice,
                Amount = unitPrice * form.Quantity,
                ChargedAt = DateTime.Now,
                SurchargeType = form.ItemType == FolioItemType.Surcharge ? SurchargeType.Other : SurchargeType.None
            };
            _db.FolioItems.Add(item);
            await _db.SaveChangesAsync();

            if (service is not null && service.IsStockManaged)
            {
                await _inventory.SellAsync(service.Id, form.Quantity, item.Id);
            }

            _audit.Log("AddFolioCharge", nameof(Folio), folio.Id.ToString(),
                newValue: $"{description} × {form.Quantity} = {item.Amount:N0} ₫");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã thêm chi phí {description}.");
    }

    public async Task<ServiceResult> VoidLineAsync(int folioItemId, string reason, int employeeId)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do hủy dòng chi phí.");
        }

        var item = await _db.FolioItems.Include(i => i.Folio).ThenInclude(f => f.Stay)
            .FirstOrDefaultAsync(i => i.Id == folioItemId);
        if (item is null)
        {
            return ServiceResult.Fail("Không tìm thấy dòng chi phí.");
        }

        if (item.Folio.IsLocked)
        {
            return ServiceResult.Fail("Folio đã khóa, không hủy dòng được.");
        }

        if (item.ItemType == FolioItemType.Room)
        {
            return ServiceResult.Fail("Không hủy được dòng tiền phòng — dùng đổi phòng/gia hạn/check-out.");
        }

        if (item.IsVoided)
        {
            return ServiceResult.Fail("Dòng này đã bị hủy.");
        }

        await _tx.ExecuteAsync(async () =>
        {
            item.IsVoided = true;
            item.VoidedAt = DateTime.Now;
            item.VoidedBy = employeeId;
            item.VoidReason = reason.Trim();

            if (item.ItemType == FolioItemType.Service && item.HotelServiceId is not null)
            {
                await _inventory.ReturnStockAsync(item.HotelServiceId.Value, item.Quantity, item.Id, "Hủy dòng dịch vụ");
            }

            _audit.Log("VoidFolioLine", nameof(FolioItem), item.Id.ToString(), reason: reason.Trim(),
                oldValue: $"{item.Description} = {item.Amount:N0} ₫");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: "Đã hủy dòng chi phí và hoàn tồn (nếu có).");
    }

    public async Task<ServiceResult> LockFolioAsync(int stayId, int employeeId)
    {
        var folio = await _db.Folios.FirstOrDefaultAsync(f => f.StayId == stayId);
        if (folio is null)
        {
            return ServiceResult.Fail("Không tìm thấy folio.");
        }

        if (folio.IsLocked)
        {
            return ServiceResult.Ok(message: "Folio đã ở trạng thái khóa.");
        }

        folio.IsLocked = true;
        folio.LockedAt = DateTime.Now;
        folio.LockedBy = employeeId;
        _audit.Log("LockFolio", nameof(Folio), folio.Id.ToString());
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: "Đã khóa folio.");
    }

    public async Task<ServiceResult> UnlockFolioAsync(int stayId, int employeeId)
    {
        var folio = await _db.Folios.Include(f => f.Invoice).FirstOrDefaultAsync(f => f.StayId == stayId);
        if (folio is null)
        {
            return ServiceResult.Fail("Không tìm thấy folio.");
        }

        if (folio.Invoice is not null && folio.Invoice.Status != InvoiceStatus.Void)
        {
            return ServiceResult.Fail("Folio đã có hóa đơn — không mở khóa được.");
        }

        folio.IsLocked = false;
        folio.LockedAt = null;
        folio.LockedBy = null;
        _audit.Log("UnlockFolio", nameof(Folio), folio.Id.ToString());
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: "Đã mở khóa folio.");
    }

    // ---------- SCR-F04 ----------

    public async Task<DiscountViewModel?> BuildDiscountAsync(int stayId, bool isAdmin)
    {
        var summary = await ComputeFolioSummaryAsync(stayId);
        if (summary is null)
        {
            return null;
        }

        var stay = await _db.Stays.AsNoTracking().Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        return new DiscountViewModel
        {
            StayId = stayId,
            RoomNumber = stay?.Room.RoomNumber ?? string.Empty,
            CurrentSubTotal = summary.RoomCharge + summary.ServiceCharge + summary.SurchargeAmount,
            ReceptionistMaxAmount = await _settings.GetDecimalAsync(SystemSettingKeys.ReceptionistMaxDiscountAmount),
            IsAdmin = isAdmin
        };
    }

    public async Task<ServiceResult> ApplyDiscountAsync(DiscountViewModel form, int employeeId, bool isAdmin)
    {
        var folio = await _db.Folios.Include(f => f.Items).Include(f => f.Stay)
            .FirstOrDefaultAsync(f => f.StayId == form.StayId);
        if (folio is null || folio.IsLocked || folio.Stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Folio đã khóa hoặc không tồn tại.");
        }

        if (string.IsNullOrWhiteSpace(form.Reason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do giảm giá.", nameof(form.Reason));
        }

        var items = folio.Items.Where(i => !i.IsVoided).ToList();
        var baseAmount = items.Where(i => i.ItemType != FolioItemType.Discount).Sum(i => i.Amount);
        var currentDiscount = -items.Where(i => i.ItemType == FolioItemType.Discount).Sum(i => i.Amount);

        var amount = form.IsPercent ? Math.Round(baseAmount * form.Value / 100m) : form.Value;
        if (amount <= 0)
        {
            return ServiceResult.Fail("Giá trị giảm phải lớn hơn 0.", nameof(form.Value));
        }

        if (currentDiscount + amount > baseAmount)
        {
            return ServiceResult.Fail("Giảm giá vượt quá tạm tính — số dư không được âm do giảm giá.", nameof(form.Value));
        }

        if (!isAdmin)
        {
            var maxAmount = await _settings.GetDecimalAsync(SystemSettingKeys.ReceptionistMaxDiscountAmount);
            var maxRate = await _settings.GetDecimalAsync(SystemSettingKeys.ReceptionistMaxDiscountRate);
            var cap = Math.Min(maxAmount, baseAmount * maxRate);
            if (amount > cap)
            {
                return ServiceResult.Fail(
                    $"Vượt hạn mức giảm giá của lễ tân ({cap:N0} ₫). Cần quản lý thực hiện.", nameof(form.Value));
            }
        }

        await _tx.ExecuteAsync(async () =>
        {
            _db.FolioItems.Add(new FolioItem
            {
                FolioId = folio.Id,
                ItemType = FolioItemType.Discount,
                Description = form.IsPercent ? $"Giảm {form.Value}%" : "Giảm giá",
                Quantity = 1,
                UnitPrice = -amount,
                Amount = -amount,
                ChargedAt = DateTime.Now,
                DiscountReason = form.Reason.Trim()
            });

            _audit.Log("ApplyDiscount", nameof(Folio), folio.Id.ToString(),
                reason: form.Reason.Trim(), newValue: $"-{amount:N0} ₫");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã giảm {amount:N0} ₫.");
    }

    // ---------- SCR-F05 ----------

    public async Task<PaymentViewModel?> BuildPaymentAsync(int stayId, int employeeId, bool isAdmin)
    {
        var folio = await _db.Folios.AsNoTracking()
            .Include(f => f.Items)
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .Include(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .FirstOrDefaultAsync(f => f.StayId == stayId);
        if (folio is null)
        {
            return null;
        }

        var deposit = await HeldDepositAsync(folio.Stay);
        var settings = await _settings.GetPricingSettingsAsync();
        var summary = BuildSummary(folio, deposit, settings);

        return new PaymentViewModel
        {
            StayId = stayId,
            RoomNumber = folio.Stay.Room.RoomNumber,
            GuestName = folio.Stay.PrimaryGuest.FullName,
            Summary = summary,
            HasOpenShift = await _shifts.GetOpenShiftAsync(employeeId) is not null,
            IsInspected = folio.Stay.IsInspected,
            IsLocked = folio.IsLocked,
            IsAdmin = isAdmin,
            AmountTendered = summary.BalanceDue > 0 ? summary.BalanceDue : 0m
        };
    }

    public async Task<(ServiceResult Result, int InvoiceId)> PayAsync(PaymentViewModel form, int employeeId, bool isAdmin)
    {
        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        if (shift is null)
        {
            return (ServiceResult.Fail("Bạn cần mở ca làm việc trước khi thu tiền (BR-10)."), 0);
        }

        var folio = await _db.Folios
            .Include(f => f.Items)
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .Include(f => f.Invoice)
            .FirstOrDefaultAsync(f => f.StayId == form.StayId);
        if (folio is null)
        {
            return (ServiceResult.Fail("Không tìm thấy folio."), 0);
        }

        if (folio.Invoice is not null && folio.Invoice.Status != InvoiceStatus.Void)
        {
            return (ServiceResult.Fail("Lượt lưu trú này đã có hóa đơn."), 0);
        }

        if (!folio.Stay.IsInspected)
        {
            return (ServiceResult.Fail("Chưa kiểm phòng & minibar — không thanh toán được (BR-08)."), 0);
        }

        if (!folio.IsLocked)
        {
            return (ServiceResult.Fail("Cần khóa folio trước khi thanh toán."), 0);
        }

        if (form.Method != PaymentMethod.Cash && string.IsNullOrWhiteSpace(form.TransactionRef))
        {
            return (ServiceResult.Fail("Chuyển khoản/Thẻ bắt buộc nhập mã giao dịch.", nameof(form.TransactionRef)), 0);
        }

        var depositHeld = await HeldDepositAsync(folio.Stay);
        var settings = await _settings.GetPricingSettingsAsync();
        var summary = BuildSummary(folio, depositHeld, settings);
        var balance = summary.BalanceDue;

        var recordDebt = form.RecordAsDebt && isAdmin && balance > 0;

        if (balance > 0 && !recordDebt)
        {
            if (form.Method == PaymentMethod.Cash && form.AmountTendered < balance)
            {
                return (ServiceResult.Fail("Tiền khách đưa nhỏ hơn số phải thu."), 0);
            }
        }

        var invoiceId = await _tx.ExecuteAsync(async () =>
        {
            var now = DateTime.Now;
            var amountPaid = recordDebt ? Math.Max(0, form.AmountTendered) : (balance > 0 ? balance : 0m);
            var debt = recordDebt ? Math.Max(0, balance - amountPaid) : 0m;

            var invoice = new Invoice
            {
                InvoiceNo = await _numbers.NextInvoiceNoAsync(now),
                FolioId = folio.Id,
                IssuedAt = now,
                IssuedBy = employeeId,
                CashierShiftId = shift.Id,
                RoomCharge = summary.RoomCharge,
                ServiceCharge = summary.ServiceCharge,
                SurchargeAmount = summary.SurchargeAmount,
                DiscountAmount = summary.DiscountAmount,
                DepositAmount = summary.DepositApplied,
                SubTotal = summary.SubTotal,
                TaxRate = summary.TaxRate,
                TaxAmount = summary.TaxAmount,
                TotalAmount = summary.Total,
                AmountPaid = amountPaid,
                DebtAmount = debt,
                Status = debt > 0 ? InvoiceStatus.Debt : InvoiceStatus.Settled
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            // Khoản thu/hoàn tiền gắn ca hiện tại.
            if (balance > 0 && amountPaid > 0)
            {
                _db.Payments.Add(new Payment
                {
                    Type = PaymentType.InvoiceSettlement,
                    Method = form.Method,
                    Amount = amountPaid,
                    TransactionRef = string.IsNullOrWhiteSpace(form.TransactionRef) ? null : form.TransactionRef.Trim(),
                    PaidAt = now,
                    CashierShiftId = shift.Id,
                    InvoiceId = invoice.Id
                });
            }
            else if (balance < 0)
            {
                // Cọc thừa: hoàn lại khách.
                _db.Payments.Add(new Payment
                {
                    Type = PaymentType.Refund,
                    Method = form.Method,
                    Amount = balance,
                    PaidAt = now,
                    CashierShiftId = shift.Id,
                    InvoiceId = invoice.Id,
                    Notes = "Hoàn cọc thừa"
                });
            }

            await ApplyDepositsAsync(folio.Stay, -Math.Min(0, balance));

            folio.Stay.Status = StayStatus.CheckedOut;
            if (folio.Stay.ActualCheckOut is null)
            {
                folio.Stay.ActualCheckOut = now;
            }

            var room = folio.Stay.Room;
            // Phòng vào hàng đợi dọn chỉ qua trạng thái Dirty (bảng buồng phòng SCR-E01 đọc theo
            // trạng thái này). KHÔNG tạo HousekeepingTask ở đây: nhóm E chỉ sinh task khi bắt đầu dọn
            // (InProgress) — tạo sẵn Pending sẽ khiến "Bắt đầu dọn" tưởng phòng đang có người dọn.
            room.Status = RoomStatus.Dirty;

            if (folio.Stay.ReservationId is not null)
            {
                var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == folio.Stay.ReservationId);
                if (reservation is not null && reservation.Status == ReservationStatus.CheckedIn)
                {
                    // Đóng đơn khi mọi lượt lưu trú của đơn đã check-out.
                    var openStays = await _db.Stays.CountAsync(s =>
                        s.ReservationId == reservation.Id && s.Status == StayStatus.CheckedIn && s.Id != folio.Stay.Id);
                    if (openStays == 0)
                    {
                        reservation.Status = ReservationStatus.CheckedOut;
                    }
                }
            }

            _audit.Log(debt > 0 ? "SettleInvoiceWithDebt" : "SettleInvoice",
                nameof(Invoice), invoice.Id.ToString(),
                newValue: $"{invoice.InvoiceNo} tổng {invoice.TotalAmount:N0} ₫, thu {amountPaid:N0} ₫, nợ {debt:N0} ₫");

            await _db.SaveChangesAsync();
            return invoice.Id;
        });

        return (ServiceResult.Ok(message: "Đã thanh toán và xuất hóa đơn."), invoiceId);
    }

    /// <summary>Đối trừ cọc vào hóa đơn: chuyển Held → Applied, ghi phần hoàn nếu cọc thừa.</summary>
    private async Task ApplyDepositsAsync(Stay stay, decimal refundTotal)
    {
        var deposits = await _db.Deposits
            .Where(d => d.Status == DepositStatus.Held
                && (d.StayId == stay.Id || (stay.ReservationId != null && d.ReservationId == stay.ReservationId)))
            .ToListAsync();

        var remainingRefund = refundTotal;
        foreach (var d in deposits)
        {
            d.Status = DepositStatus.Applied;
            if (remainingRefund > 0)
            {
                var share = Math.Min(remainingRefund, d.Amount);
                d.RefundedAmount = share;
                remainingRefund -= share;
            }
        }
    }

    // ---------- SCR-F06 ----------

    public async Task<InvoiceViewModel?> BuildInvoiceAsync(int invoiceId)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Folio).ThenInclude(f => f.Items)
            .Include(i => i.Folio).ThenInclude(f => f.Stay).ThenInclude(s => s.Room)
            .Include(i => i.Folio).ThenInclude(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return null;
        }

        var stay = invoice.Folio.Stay;

        return new InvoiceViewModel
        {
            Invoice = invoice,
            HotelName = await _settings.GetStringAsync(SystemSettingKeys.HotelName),
            HotelAddress = await _settings.GetStringAsync(SystemSettingKeys.HotelAddress),
            HotelPhone = await _settings.GetStringAsync(SystemSettingKeys.HotelPhone),
            HotelTaxCode = await _settings.GetStringAsync(SystemSettingKeys.HotelTaxCode),
            GuestName = stay.PrimaryGuest.FullName,
            GuestIdNumber = stay.PrimaryGuest.IdNumber,
            RoomNumber = stay.Room.RoomNumber,
            ActualCheckIn = stay.ActualCheckIn,
            ActualCheckOut = stay.ActualCheckOut,
            Nights = stay.Nights,
            Payments = invoice.Payments.OrderBy(p => p.PaidAt).ToList(),
            Lines = invoice.Folio.Items
                .OrderBy(i => i.ChargedAt).ThenBy(i => i.Id)
                .Select(i => new FolioLineView
                {
                    Id = i.Id,
                    ChargedAt = i.ChargedAt,
                    ItemType = i.ItemType,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    IsVoided = i.IsVoided
                }).ToList()
        };
    }

    // ---------- SCR-F07 ----------

    public async Task<VoidInvoiceViewModel?> BuildVoidAsync(int invoiceId, int employeeId)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Folio).ThenInclude(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null || invoice.Status == InvoiceStatus.Void)
        {
            return null;
        }

        return new VoidInvoiceViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            TotalAmount = invoice.TotalAmount,
            GuestName = invoice.Folio.Stay.PrimaryGuest.FullName,
            HasOpenShift = await _shifts.GetOpenShiftAsync(employeeId) is not null
        };
    }

    public async Task<ServiceResult> VoidInvoiceAsync(VoidInvoiceViewModel form, int employeeId)
    {
        if (!form.Confirmed)
        {
            return ServiceResult.Fail("Vui lòng xác nhận hiểu rằng thao tác không thể hoàn tác.");
        }

        if (string.IsNullOrWhiteSpace(form.Reason) || form.Reason.Trim().Length < 10)
        {
            return ServiceResult.Fail("Lý do hủy phải từ 10 ký tự.", nameof(form.Reason));
        }

        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        if (shift is null)
        {
            return ServiceResult.Fail("Cần mở ca làm việc để hủy hóa đơn (điều chỉnh gắn ca hiện tại).");
        }

        var invoice = await _db.Invoices.Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == form.InvoiceId);
        if (invoice is null)
        {
            return ServiceResult.Fail("Không tìm thấy hóa đơn.");
        }

        if (invoice.Status == InvoiceStatus.Void)
        {
            return ServiceResult.Fail("Hóa đơn đã bị hủy.");
        }

        await _tx.ExecuteAsync(async () =>
        {
            invoice.Status = InvoiceStatus.Void;
            invoice.VoidedAt = DateTime.Now;
            invoice.VoidedBy = employeeId;
            invoice.VoidReason = form.Reason.Trim();

            // Payment đối ứng (âm) trung hòa khoản đã thu, gắn ca hiện tại — không sửa ngược ca cũ.
            var settled = invoice.Payments.Where(p => p.Type == PaymentType.InvoiceSettlement).Sum(p => p.Amount);
            if (settled != 0)
            {
                _db.Payments.Add(new Payment
                {
                    Type = PaymentType.VoidAdjustment,
                    Method = PaymentMethod.Cash,
                    Amount = -settled,
                    PaidAt = DateTime.Now,
                    CashierShiftId = shift.Id,
                    InvoiceId = invoice.Id,
                    Notes = "Điều chỉnh do hủy hóa đơn"
                });
            }

            _audit.Log("VoidInvoice", nameof(Invoice), invoice.Id.ToString(),
                reason: form.Reason.Trim(), oldValue: $"{invoice.InvoiceNo} {invoice.TotalAmount:N0} ₫");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: $"Đã hủy hóa đơn {invoice.InvoiceNo}.");
    }

    // ---------- E02 kiểm minibar ----------

    public async Task<MinibarViewModel?> BuildMinibarAsync(int stayId)
    {
        var stay = await _db.Stays.AsNoTracking().Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay is null)
        {
            return null;
        }

        var minibarItems = await _db.HotelServices.AsNoTracking()
            .Where(s => s.IsActive && s.IsStockManaged && s.Category == ServiceCategory.Minibar)
            .OrderBy(s => s.Code)
            .Select(s => new MinibarLineInput
            {
                HotelServiceId = s.Id,
                ServiceName = s.Name,
                Unit = s.Unit,
                UnitPrice = s.UnitPrice,
                StockQuantity = s.StockQuantity,
                Quantity = 0
            })
            .ToListAsync();

        return new MinibarViewModel
        {
            StayId = stayId,
            RoomNumber = stay.Room.RoomNumber,
            AlreadyInspected = stay.IsInspected,
            Lines = minibarItems
        };
    }

    public async Task<ServiceResult> SaveMinibarAsync(MinibarViewModel form, int employeeId)
    {
        var folio = await _db.Folios.Include(f => f.Stay)
            .FirstOrDefaultAsync(f => f.StayId == form.StayId);
        if (folio is null || folio.Stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail("Không tìm thấy lượt lưu trú đang mở.");
        }

        var used = (form.Lines ?? new List<MinibarLineInput>()).Where(l => l.Quantity > 0).ToList();

        await _tx.ExecuteAsync(async () =>
        {
            foreach (var line in used)
            {
                var service = await _db.HotelServices.FirstOrDefaultAsync(s => s.Id == line.HotelServiceId);
                if (service is null)
                {
                    continue;
                }

                var item = new FolioItem
                {
                    FolioId = folio.Id,
                    ItemType = FolioItemType.Service,
                    HotelServiceId = service.Id,
                    Description = service.Name,
                    Quantity = line.Quantity,
                    UnitPrice = service.UnitPrice,
                    Amount = service.UnitPrice * line.Quantity,
                    ChargedAt = DateTime.Now
                };
                _db.FolioItems.Add(item);
                await _db.SaveChangesAsync();

                await _inventory.SellAsync(service.Id, line.Quantity, item.Id);
            }

            folio.Stay.IsInspected = true;
            folio.Stay.InspectedAt = DateTime.Now;
            folio.Stay.InspectedBy = employeeId;

            _audit.Log("InspectMinibar", nameof(Stay), folio.Stay.Id.ToString(),
                newValue: used.Count == 0 ? "Không dùng minibar" : $"{used.Count} mặt hàng minibar");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(message: "Đã ghi nhận kiểm phòng & minibar.");
    }

    private async Task<IReadOnlyList<SelectListItem>> ServiceOptionsAsync()
        => await _db.HotelServices.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.IsStockManaged
                    ? $"{s.Code} — {s.Name} ({s.UnitPrice:N0} ₫, tồn {s.StockQuantity})"
                    : $"{s.Code} — {s.Name} ({s.UnitPrice:N0} ₫)"
            })
            .ToListAsync();

    // ---------- Giải thích lỗi ----------
    // Chỉ chạy khi không mở được màn hình, để nói đúng lý do thay vì trả 404 trắng.

    public async Task<StayStatus?> GetStayStatusAsync(int stayId)
        => await _db.Stays.AsNoTracking()
            .Where(s => s.Id == stayId)
            .Select(s => (StayStatus?)s.Status)
            .FirstOrDefaultAsync();

    public async Task<InvoiceStatus?> GetInvoiceStatusAsync(int invoiceId)
        => await _db.Invoices.AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => (InvoiceStatus?)i.Status)
            .FirstOrDefaultAsync();
}
