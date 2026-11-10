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

    /// <summary>Chuyển 1 dòng chi phí hoặc gộp toàn bộ chi phí từ phòng này sang Folio phòng khác.</summary>
    Task<ServiceResult> TransferFolioAsync(int fromStayId, int toStayId, int? folioItemId, bool includeRoomCharges, string reason, int employeeId);

    /// <summary>Màn hình thanh toán gộp (Master Folio / Group Checkout) cho đoàn hoặc nhiều phòng.</summary>
    Task<GroupPaymentViewModel?> BuildGroupPaymentAsync(int? reservationId, string? stayIdsCsv, int employeeId, bool isAdmin);

    /// <summary>Kiểm phòng & khóa toàn bộ Folio của đoàn/nhóm phòng trong 1 thao tác.</summary>
    Task<ServiceResult> PrepareGroupCheckoutAsync(int? reservationId, string? stayIdsCsv, int employeeId);

    /// <summary>Thanh toán gộp tất cả các phòng trong đoàn/nhóm phòng.</summary>
    Task<(ServiceResult Result, int FirstInvoiceId)> PayGroupAsync(GroupPaymentViewModel form, int employeeId, bool isAdmin);

    /// <summary>Thu nợ cho hóa đơn đang ở trạng thái Công nợ (Debt).</summary>
    Task<ServiceResult> SettleDebtAsync(int invoiceId, PaymentMethod method, string? transactionRef, int employeeId);

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
    private readonly IVietQrService _vietQr;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;

    public BillingService(
        HotelDbContext db, IAuditService audit, IPricingService pricing, INumberSequenceService numbers,
        ITransactionRunner tx, IShiftService shifts, ISettingsReader settings, IInventoryService inventory,
        IVietQrService vietQr, INotificationService notifications, IEmailService email)
    {
        _db = db;
        _audit = audit;
        _pricing = pricing;
        _numbers = numbers;
        _tx = tx;
        _shifts = shifts;
        _settings = settings;
        _inventory = inventory;
        _vietQr = vietQr;
        _notifications = notifications;
        _email = email;
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
            BalanceDue = total.BalanceDue,
            RoomLines = items
                .Where(i => i.ItemType == FolioItemType.Room)
                .Select(i => new RoomLineView
                {
                    Description = i.Description,
                    Amount = i.Amount
                })
                .ToList(),
            SurchargeLines = items
                .Where(i => i.ItemType == FolioItemType.Surcharge)
                .Select(i => new SurchargeLineView
                {
                    Description = i.Description,
                    Amount = i.Amount,
                    IsPending = false
                })
                .ToList()
        };

    }

    /// <summary>
    /// Tính tổng tiền cọc đang giữ (Held) áp dụng cho lượt lưu trú này.
    /// Cọc gắn trực tiếp với StayId luôn được tính; cọc gắn cấp ReservationId (StayId == null)
    /// được phân bổ vào lượt lưu trú đang mở đầu tiên của đơn để tránh nhân đôi khi đoàn nhiều phòng.
    /// </summary>
    private async Task<decimal> HeldDepositAsync(Stay stay)
    {
        var direct = await _db.Deposits.AsNoTracking()
            .Where(d => d.Status == DepositStatus.Held && d.StayId == stay.Id)
            .SumAsync(d => (decimal?)d.Amount) ?? 0m;

        if (stay.ReservationId is null)
        {
            return direct;
        }

        var firstOpenStayId = await _db.Stays.AsNoTracking()
            .Where(s => s.ReservationId == stay.ReservationId && s.Status == StayStatus.CheckedIn)
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (firstOpenStayId == stay.Id || firstOpenStayId is null)
        {
            var resDeposit = await _db.Deposits.AsNoTracking()
                .Where(d => d.Status == DepositStatus.Held && d.StayId == null && d.ReservationId == stay.ReservationId)
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;
            return direct + resDeposit;
        }

        return direct;
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
            .Include(f => f.Stay).ThenInclude(s => s.Reservation)
            .ToListAsync();

        var keyword = filter.Keyword?.Trim();
        var items = new List<OpenFolioListItem>();
        foreach (var f in openStays)
        {
            if (!string.IsNullOrWhiteSpace(keyword)
                && !f.Stay.Room.RoomNumber.Contains(keyword)
                && !f.Stay.PrimaryGuest.FullName.Contains(keyword)
                && !f.Code.Contains(keyword)
                && (f.Stay.Reservation == null || !f.Stay.Reservation.Code.Contains(keyword)))
            {
                continue;
            }

            var deposit = await HeldDepositAsync(f.Stay);
            var summary = BuildSummary(f, deposit, settings);
            items.Add(new OpenFolioListItem
            {
                StayId = f.StayId,
                ReservationId = f.Stay.ReservationId,
                ReservationCode = f.Stay.Reservation?.Code,
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
            .Include(f => f.Stay).ThenInclude(s => s.Reservation)
            .Include(f => f.Invoice)
            .FirstOrDefaultAsync(f => f.StayId == stayId);

        if (folio is null)
        {
            return null;
        }

        var deposit = await HeldDepositAsync(folio.Stay);
        var settings = await _settings.GetPricingSettingsAsync();

        var otherOpenStays = await _db.Stays.AsNoTracking()
            .Where(s => s.Id != stayId && s.Status == StayStatus.CheckedIn && s.Folio != null && !s.Folio.IsLocked)
            .Include(s => s.Room)
            .Include(s => s.PrimaryGuest)
            .Include(s => s.Folio)
            .OrderBy(s => s.Room.RoomNumber)
            .Select(s => new TransferRoomTargetOption
            {
                StayId = s.Id,
                RoomNumber = s.Room.RoomNumber,
                GuestName = s.PrimaryGuest.FullName,
                FolioCode = s.Folio != null ? s.Folio.Code : string.Empty,
                IsSameReservation = folio.Stay.ReservationId != null && s.ReservationId == folio.Stay.ReservationId
            })
            .ToListAsync();

        return new FolioDetailViewModel
        {
            StayId = stayId,
            ReservationId = folio.Stay.ReservationId,
            ReservationCode = folio.Stay.Reservation?.Code,
            RoomNumber = folio.Stay.Room.RoomNumber,
            RoomTypeName = folio.Stay.Room.RoomType.Name,
            GuestName = folio.Stay.PrimaryGuest.FullName,
            ActualCheckIn = folio.Stay.ActualCheckIn,
            ExpectedCheckOut = folio.Stay.ExpectedCheckOut,
            Nights = folio.Stay.Nights,
            StayStatus = folio.Stay.Status,
            InvoiceId = folio.Invoice?.Id,
            Summary = BuildSummary(folio, deposit, settings),
            OtherOpenStays = otherOpenStays,
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

        VietQrPayload? vietQr = null;
        if (summary.BalanceDue > 0)
        {
            vietQr = await _vietQr.BuildAsync(
                summary.BalanceDue,
                $"TT P{folio.Stay.Room.RoomNumber} {folio.Code}",
                $"Thanh toán Folio {folio.Code} - Phòng {folio.Stay.Room.RoomNumber}");
        }

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
            AmountTendered = summary.BalanceDue > 0 ? summary.BalanceDue : 0m,
            VietQr = vietQr
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

        decimal finalAmountPaid = 0m;
        var invoiceId = await _tx.ExecuteAsync(async () =>
        {
            var now = DateTime.Now;
            var amountPaid = recordDebt ? Math.Max(0, form.AmountTendered) : (balance > 0 ? balance : 0m);
            var debt = recordDebt ? Math.Max(0, balance - amountPaid) : 0m;
            finalAmountPaid = amountPaid;

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

            // Đánh dấu các khoản cọc Held đã áp dụng thành Applied.
            await ApplyHeldDepositsAsync(folio.Stay);

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

        try
        {
            await _email.SendInvoiceEmailAsync(invoiceId);
            if (finalAmountPaid > 0)
            {
                await _notifications.NotifyPaymentReceivedAsync(
                    folio.Code, finalAmountPaid, form.Method.ToString(), $"/Billing/Invoice/{invoiceId}");
            }
        }
        catch
        {
            // Không làm gián đoạn luồng thu ngân nếu gửi email/thông báo gặp lỗi
        }

        return (ServiceResult.Ok(message: "Đã thanh toán và xuất hóa đơn."), invoiceId);
    }

    private async Task ApplyHeldDepositsAsync(Stay stay)
    {
        var deposits = await _db.Deposits
            .Where(d => d.Status == DepositStatus.Held
                && (d.StayId == stay.Id || (stay.ReservationId != null && d.StayId == null && d.ReservationId == stay.ReservationId)))
            .ToListAsync();

        foreach (var d in deposits)
        {
            d.Status = DepositStatus.Applied;
            d.StayId ??= stay.Id;
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

        VietQrPayload? debtQr = null;
        if (invoice.Status == InvoiceStatus.Debt && invoice.DebtAmount > 0)
        {
            debtQr = await _vietQr.BuildAsync(
                invoice.DebtAmount,
                $"THUNO {invoice.InvoiceNo}",
                $"Thu nợ Hóa đơn {invoice.InvoiceNo} - Phòng {stay.Room.RoomNumber}");
        }

        return new InvoiceViewModel
        {
            Invoice = invoice,
            HotelName = await _settings.GetStringAsync(SystemSettingKeys.HotelName),
            HotelAddress = await _settings.GetStringAsync(SystemSettingKeys.HotelAddress),
            HotelPhone = await _settings.GetStringAsync(SystemSettingKeys.HotelPhone),
            HotelTaxCode = await _settings.GetStringAsync(SystemSettingKeys.HotelTaxCode),
            GuestName = stay.PrimaryGuest.FullName,
            GuestIdNumber = stay.PrimaryGuest.IdNumber,
            GuestEmail = stay.PrimaryGuest.Email,
            RoomNumber = stay.Room.RoomNumber,
            ActualCheckIn = stay.ActualCheckIn,
            ActualCheckOut = stay.ActualCheckOut,
            Nights = stay.Nights,
            DebtVietQr = debtQr,
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

    // ---------- Chuyển / Gộp chi phí liên phòng & Thanh toán gộp (Master Folio / Group Checkout) ----------

    public async Task<ServiceResult> TransferFolioAsync(
        int fromStayId, int toStayId, int? folioItemId, bool includeRoomCharges, string reason, int employeeId)
    {
        if (fromStayId == toStayId)
        {
            return ServiceResult.Fail("Phòng nhận chi phí phải khác phòng chuyển.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do chuyển/gộp chi phí.");
        }

        var sourceFolio = await _db.Folios
            .Include(f => f.Items)
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(f => f.StayId == fromStayId);

        var targetFolio = await _db.Folios
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(f => f.StayId == toStayId);

        if (sourceFolio is null || targetFolio is null)
        {
            return ServiceResult.Fail("Không tìm thấy Folio của phòng nguồn hoặc phòng đích.");
        }

        if (sourceFolio.IsLocked || sourceFolio.Stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail($"Folio phòng {sourceFolio.Stay.Room.RoomNumber} đã khóa hoặc đã trả phòng.");
        }

        if (targetFolio.IsLocked || targetFolio.Stay.Status != StayStatus.CheckedIn)
        {
            return ServiceResult.Fail($"Folio phòng {targetFolio.Stay.Room.RoomNumber} đã khóa hoặc đã trả phòng.");
        }

        List<FolioItem> toMove;
        if (folioItemId.HasValue && folioItemId.Value > 0)
        {
            var single = sourceFolio.Items.FirstOrDefault(i => i.Id == folioItemId.Value && !i.IsVoided);
            if (single is null)
            {
                return ServiceResult.Fail("Không tìm thấy dòng chi phí cần chuyển (hoặc đã hủy).");
            }
            toMove = [single];
        }
        else
        {
            toMove = sourceFolio.Items
                .Where(i => !i.IsVoided && (includeRoomCharges || i.ItemType != FolioItemType.Room))
                .ToList();
        }

        if (toMove.Count == 0)
        {
            return ServiceResult.Fail("Không có dòng chi phí hợp lệ nào để chuyển sang phòng khác.");
        }

        var trimmedReason = reason.Trim();
        var srcRoomNo = sourceFolio.Stay.Room.RoomNumber;
        var dstRoomNo = targetFolio.Stay.Room.RoomNumber;
        decimal movedTotal = 0m;

        await _tx.ExecuteAsync(async () =>
        {
            var now = DateTime.Now;
            foreach (var item in toMove)
            {
                item.IsVoided = true;
                item.VoidedAt = now;
                item.VoidedBy = employeeId;
                item.VoidReason = $"Chuyển sang P.{dstRoomNo}: {trimmedReason}";

                var prefix = $"[Từ P.{srcRoomNo}] ";
                var newDesc = item.Description.StartsWith("[Từ P.")
                    ? item.Description
                    : (prefix + item.Description);
                if (newDesc.Length > 250)
                {
                    newDesc = newDesc[..250];
                }

                _db.FolioItems.Add(new FolioItem
                {
                    FolioId = targetFolio.Id,
                    ItemType = item.ItemType,
                    HotelServiceId = item.HotelServiceId,
                    Description = newDesc,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Amount,
                    ChargedAt = now,
                    SurchargeType = item.SurchargeType,
                    DiscountReason = item.DiscountReason
                });

                movedTotal += item.Amount;
            }

            _audit.Log("TransferFolioCharge", nameof(Folio), $"{sourceFolio.Id}->{targetFolio.Id}",
                reason: trimmedReason,
                newValue: $"Chuyển {toMove.Count} mục ({movedTotal:N0} ₫) từ P.{srcRoomNo} sang P.{dstRoomNo}");

            await _db.SaveChangesAsync();
        });

        return ServiceResult.Ok(
            message: $"Đã chuyển {toMove.Count} mục chi phí ({movedTotal:N0} ₫) từ P.{srcRoomNo} sang Folio P.{dstRoomNo}.");
    }

    private async Task<List<Folio>> LoadGroupFoliosAsync(int? reservationId, string? stayIdsCsv, bool asNoTracking)
    {
        var query = asNoTracking ? _db.Folios.AsNoTracking() : _db.Folios.AsQueryable();
        query = query
            .Include(f => f.Items)
            .Include(f => f.Invoice)
            .Include(f => f.Stay).ThenInclude(s => s.Room)
            .Include(f => f.Stay).ThenInclude(s => s.PrimaryGuest)
            .Include(f => f.Stay).ThenInclude(s => s.Reservation)
            .Where(f => f.Stay.Status == StayStatus.CheckedIn);

        if (!string.IsNullOrWhiteSpace(stayIdsCsv))
        {
            var ids = stayIdsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count > 0)
            {
                return await query.Where(f => ids.Contains(f.StayId))
                    .OrderBy(f => f.Stay.Room.RoomNumber)
                    .ToListAsync();
            }
        }

        if (reservationId.HasValue && reservationId.Value > 0)
        {
            return await query.Where(f => f.Stay.ReservationId == reservationId.Value)
                .OrderBy(f => f.Stay.Room.RoomNumber)
                .ToListAsync();
        }

        return [];
    }

    public async Task<GroupPaymentViewModel?> BuildGroupPaymentAsync(
        int? reservationId, string? stayIdsCsv, int employeeId, bool isAdmin)
    {
        var folios = await LoadGroupFoliosAsync(reservationId, stayIdsCsv, asNoTracking: true);
        if (folios.Count == 0)
        {
            return null;
        }

        var settings = await _settings.GetPricingSettingsAsync();
        var stayItems = new List<GroupStaySummaryItem>();

        foreach (var f in folios)
        {
            var dep = await HeldDepositAsync(f.Stay);
            var sum = BuildSummary(f, dep, settings);
            stayItems.Add(new GroupStaySummaryItem
            {
                StayId = f.StayId,
                RoomNumber = f.Stay.Room.RoomNumber,
                GuestName = f.Stay.PrimaryGuest.FullName,
                FolioCode = f.Code,
                IsInspected = f.Stay.IsInspected,
                IsLocked = f.IsLocked,
                RoomCharge = sum.RoomCharge,
                ServiceCharge = sum.ServiceCharge,
                SurchargeAmount = sum.SurchargeAmount,
                DiscountAmount = sum.DiscountAmount,
                TaxAmount = sum.TaxAmount,
                Total = sum.Total,
                DepositApplied = sum.DepositApplied,
                BalanceDue = sum.BalanceDue
            });
        }

        var firstStay = folios[0].Stay;
        var resId = reservationId ?? (folios.Select(f => f.Stay.ReservationId).Distinct().Count() == 1 ? firstStay.ReservationId : null);
        var resCode = resId.HasValue ? firstStay.Reservation?.Code : null;
        var totalBalanceDue = stayItems.Sum(i => i.BalanceDue);

        VietQrPayload? qr = null;
        if (totalBalanceDue > 0)
        {
            var memo = !string.IsNullOrWhiteSpace(resCode)
                ? $"TT DOAN {resCode}"
                : $"TT GOP {stayItems.Count} PHONG";
            qr = await _vietQr.BuildAsync(
                totalBalanceDue,
                memo,
                $"Thanh toán gộp {stayItems.Count} phòng ({string.Join(", ", stayItems.Select(s => "P." + s.RoomNumber))})");
        }

        return new GroupPaymentViewModel
        {
            ReservationId = resId,
            ReservationCode = resCode,
            LeaderGuestName = firstStay.PrimaryGuest.FullName,
            StayIdsCsv = string.Join(",", stayItems.Select(s => s.StayId)),
            Stays = stayItems,
            TotalRoomCharge = stayItems.Sum(i => i.RoomCharge),
            TotalServiceCharge = stayItems.Sum(i => i.ServiceCharge),
            TotalSurcharge = stayItems.Sum(i => i.SurchargeAmount),
            TotalDiscount = stayItems.Sum(i => i.DiscountAmount),
            TotalTax = stayItems.Sum(i => i.TaxAmount),
            GrandTotal = stayItems.Sum(i => i.Total),
            TotalDepositApplied = stayItems.Sum(i => i.DepositApplied),
            TotalBalanceDue = totalBalanceDue,
            HasOpenShift = await _shifts.GetOpenShiftAsync(employeeId) is not null,
            AllInspected = stayItems.All(i => i.IsInspected),
            AllLocked = stayItems.All(i => i.IsLocked),
            IsAdmin = isAdmin,
            AmountTendered = totalBalanceDue > 0 ? totalBalanceDue : 0m,
            VietQr = qr
        };
    }

    public async Task<ServiceResult> PrepareGroupCheckoutAsync(int? reservationId, string? stayIdsCsv, int employeeId)
    {
        var folios = await LoadGroupFoliosAsync(reservationId, stayIdsCsv, asNoTracking: false);
        if (folios.Count == 0)
        {
            return ServiceResult.Fail("Không tìm thấy phòng đang lưu trú trong nhóm để chuẩn bị check-out.");
        }

        var now = DateTime.Now;
        foreach (var f in folios)
        {
            if (!f.Stay.IsInspected)
            {
                f.Stay.IsInspected = true;
                f.Stay.InspectedAt = now;
                f.Stay.InspectedBy = employeeId;
            }

            if (!f.IsLocked)
            {
                f.IsLocked = true;
                f.LockedAt = now;
                f.LockedBy = employeeId;
            }
        }

        _audit.Log("PrepareGroupCheckout", nameof(Stay),
            string.Join(",", folios.Select(f => f.StayId)),
            newValue: $"Đã kiểm phòng & khóa Folio cho {folios.Count} phòng ({string.Join(", ", folios.Select(f => "P." + f.Stay.Room.RoomNumber))})");

        await _db.SaveChangesAsync();
        return ServiceResult.Ok(message: $"Đã xác nhận kiểm phòng & khóa tất cả {folios.Count} Folio. Sẵn sàng thanh toán gộp!");
    }

    public async Task<(ServiceResult Result, int FirstInvoiceId)> PayGroupAsync(
        GroupPaymentViewModel form, int employeeId, bool isAdmin)
    {
        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        if (shift is null)
        {
            return (ServiceResult.Fail("Bạn cần mở ca làm việc trước khi thu tiền gộp (BR-10)."), 0);
        }

        var folios = await LoadGroupFoliosAsync(form.ReservationId, form.StayIdsCsv, asNoTracking: false);
        if (folios.Count == 0)
        {
            return (ServiceResult.Fail("Không tìm thấy các phòng cần thanh toán gộp."), 0);
        }

        if (folios.Any(f => !f.Stay.IsInspected || !f.IsLocked))
        {
            return (ServiceResult.Fail("Vui lòng bấm 'Kiểm phòng & Khóa tất cả Folio' trước khi thanh toán gộp."), 0);
        }

        if (form.Method != PaymentMethod.Cash && string.IsNullOrWhiteSpace(form.TransactionRef))
        {
            return (ServiceResult.Fail("Chuyển khoản/Thẻ bắt buộc nhập mã giao dịch.", nameof(form.TransactionRef)), 0);
        }

        var settings = await _settings.GetPricingSettingsAsync();
        var summaries = new List<(Folio Folio, BillingFolioSummary Summary)>();
        foreach (var f in folios)
        {
            var dep = await HeldDepositAsync(f.Stay);
            summaries.Add((f, BuildSummary(f, dep, settings)));
        }

        var totalBalance = summaries.Sum(x => x.Summary.BalanceDue);
        if (totalBalance > 0 && form.Method == PaymentMethod.Cash && form.AmountTendered < totalBalance)
        {
            return (ServiceResult.Fail("Tiền khách đưa nhỏ hơn tổng số phải thu của đoàn."), 0);
        }

        var firstInvoiceId = await _tx.ExecuteAsync(async () =>
        {
            var now = DateTime.Now;
            var createdInvoiceIds = new List<int>();

            foreach (var (folio, summary) in summaries)
            {
                var balance = summary.BalanceDue;
                var amountPaid = balance > 0 ? balance : 0m;

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
                    DebtAmount = 0m,
                    Status = InvoiceStatus.Settled
                };
                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync();
                createdInvoiceIds.Add(invoice.Id);

                if (amountPaid > 0)
                {
                    _db.Payments.Add(new Payment
                    {
                        Type = PaymentType.InvoiceSettlement,
                        Method = form.Method,
                        Amount = amountPaid,
                        TransactionRef = string.IsNullOrWhiteSpace(form.TransactionRef) ? null : form.TransactionRef.Trim(),
                        PaidAt = now,
                        CashierShiftId = shift.Id,
                        InvoiceId = invoice.Id,
                        Notes = $"Thanh toán gộp {folios.Count} phòng"
                    });
                }

                await ApplyHeldDepositsAsync(folio.Stay);

                folio.Stay.Status = StayStatus.CheckedOut;
                folio.Stay.ActualCheckOut ??= now;
                folio.Stay.Room.Status = RoomStatus.Dirty;
            }

            // Đóng các Reservation liên quan nếu không còn phòng nào đang CheckedIn
            var reservationIds = folios
                .Where(f => f.Stay.ReservationId.HasValue)
                .Select(f => f.Stay.ReservationId!.Value)
                .Distinct()
                .ToList();

            var checkedOutStayIds = folios.Select(f => f.StayId).ToHashSet();
            foreach (var resId in reservationIds)
            {
                var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == resId);
                if (reservation is not null && reservation.Status == ReservationStatus.CheckedIn)
                {
                    var remainingOpen = await _db.Stays.CountAsync(s =>
                        s.ReservationId == resId && s.Status == StayStatus.CheckedIn && !checkedOutStayIds.Contains(s.Id));
                    if (remainingOpen == 0)
                    {
                        reservation.Status = ReservationStatus.CheckedOut;
                    }
                }
            }

            _audit.Log("SettleGroupInvoices", nameof(Invoice), string.Join(",", createdInvoiceIds),
                newValue: $"Thanh toán gộp {folios.Count} phòng, tổng thu {totalBalance:N0} ₫");

            await _db.SaveChangesAsync();
            return createdInvoiceIds.FirstOrDefault();
        });

        try
        {
            if (firstInvoiceId > 0)
            {
                await _email.SendInvoiceEmailAsync(firstInvoiceId);
                if (totalBalance > 0)
                {
                    await _notifications.NotifyPaymentReceivedAsync(
                        $"Đoàn ({folios.Count} phòng)", totalBalance, form.Method.ToString(), $"/Billing/Invoice/{firstInvoiceId}");
                }
            }
        }
        catch
        {
            // Ignore notification errors
        }

        return (ServiceResult.Ok(
            message: $"Đã thanh toán gộp và trả phòng thành công cho {folios.Count} phòng ({totalBalance:N0} ₫)."), firstInvoiceId);
    }

    public async Task<ServiceResult> SettleDebtAsync(
        int invoiceId, PaymentMethod method, string? transactionRef, int employeeId)
    {
        var shift = await _shifts.GetOpenShiftAsync(employeeId);
        if (shift is null)
        {
            return ServiceResult.Fail("Bạn cần mở ca làm việc trước khi thu nợ hóa đơn (BR-10).");
        }

        if (method != PaymentMethod.Cash && string.IsNullOrWhiteSpace(transactionRef))
        {
            return ServiceResult.Fail("Chuyển khoản/Thẻ bắt buộc nhập mã giao dịch.");
        }

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return ServiceResult.Fail("Không tìm thấy hóa đơn.");
        }

        if (invoice.Status != InvoiceStatus.Debt || invoice.DebtAmount <= 0)
        {
            return ServiceResult.Fail("Hóa đơn này không có khoản nợ cần thu.");
        }

        var debtToCollect = invoice.DebtAmount;
        await _tx.ExecuteAsync(async () =>
        {
            var now = DateTime.Now;
            _db.Payments.Add(new Payment
            {
                Type = PaymentType.InvoiceSettlement,
                Method = method,
                Amount = debtToCollect,
                TransactionRef = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                PaidAt = now,
                CashierShiftId = shift.Id,
                InvoiceId = invoice.Id,
                Notes = "Thu dứt điểm công nợ hóa đơn"
            });

            invoice.AmountPaid += debtToCollect;
            invoice.DebtAmount = 0m;
            invoice.Status = InvoiceStatus.Settled;

            _audit.Log("SettleInvoiceDebt", nameof(Invoice), invoice.Id.ToString(),
                newValue: $"Thu nợ {debtToCollect:N0} ₫ cho HĐ {invoice.InvoiceNo}");

            await _db.SaveChangesAsync();
        });

        try
        {
            await _notifications.NotifyPaymentReceivedAsync(
                invoice.InvoiceNo, debtToCollect, method.ToString(), $"/Billing/Invoice/{invoice.Id}");
        }
        catch
        {
            // Ignore notification errors
        }

        return ServiceResult.Ok(message: $"Đã thu hết nợ {debtToCollect:N0} ₫ cho hóa đơn {invoice.InvoiceNo}.");
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
