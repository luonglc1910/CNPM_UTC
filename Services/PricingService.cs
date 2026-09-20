using HotelManagement.Web.Models;

namespace HotelManagement.Web.Services;

/// <summary>
/// Ảnh chụp các tham số cấu hình phục vụ tính tiền — lấy từ <see cref="ISettingsReader"/>.
/// Tỷ lệ giữ dạng thô (0.08, 0.5...), giờ giữ dạng <see cref="TimeOnly"/>.
/// </summary>
public class PricingSettings
{
    public decimal VatRate { get; init; }
    public int RoundingUnit { get; init; } = 1000;

    public TimeOnly StandardCheckInTime { get; init; } = new(14, 0);
    public TimeOnly StandardCheckOutTime { get; init; } = new(12, 0);

    public int EarlyCheckInBoundaryHour { get; init; } = 9;
    public decimal EarlyCheckInBeforeBoundaryRate { get; init; }
    public decimal EarlyCheckInToStandardRate { get; init; }

    public int LateCheckOutTier1EndHour { get; init; } = 15;
    public decimal LateCheckOutTier1Rate { get; init; }
    public decimal LateCheckOutTier2Rate { get; init; }
    public int LateCheckOutFullNightHour { get; init; } = 18;

    public int DepositNights { get; init; } = 1;
    public int HoldUntilHour { get; init; } = 18;
    public decimal CancelFeeOver48hRate { get; init; }
    public decimal CancelFee24To48hRate { get; init; }
    public decimal CancelFeeUnder24hRate { get; init; }

    public int ChildAgeLimit { get; init; } = 12;
}

/// <summary>Một dòng phụ thu tính được — BR-03.</summary>
public class SurchargeLine
{
    public SurchargeType Type { get; init; }
    public decimal Amount { get; init; }

    /// <summary>Số đêm cộng thêm (chỉ dùng cho trả phòng sau mốc tính-thêm-1-đêm).</summary>
    public int ExtraNights { get; init; }

    public string Description { get; init; } = string.Empty;
}

/// <summary>Kết quả tổng hợp folio/hóa đơn — BR-04.</summary>
public class FolioTotal
{
    public decimal RoomCharge { get; init; }
    public decimal ServiceCharge { get; init; }
    public decimal SurchargeAmount { get; init; }
    public decimal DiscountAmount { get; init; }

    /// <summary>Cơ sở tính VAT = phòng + dịch vụ + phụ thu − giảm giá (KHÔNG trừ cọc — cọc là trả trước, không giảm doanh thu chịu thuế).</summary>
    public decimal SubTotal { get; init; }

    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }

    /// <summary>Tổng hóa đơn đã gồm VAT và đã làm tròn — BR-04.</summary>
    public decimal Total { get; init; }

    public decimal DepositApplied { get; init; }

    /// <summary>Số dư khách còn phải trả = Tổng − cọc. Âm nghĩa là phải hoàn lại khách.</summary>
    public decimal BalanceDue { get; init; }
}

/// <summary>
/// Tính tiền phòng, phụ thu, VAT, làm tròn và phí hủy — BR-02, BR-03, BR-04, BR-05.
///
/// Cố ý viết dạng **logic thuần**: mọi tham số truyền vào tường minh, không đụng DB, không đọc
/// thời gian hệ thống ẩn — để tính lại được y hệt ở mọi màn hình (tra phòng, check-in, check-out,
/// folio, hủy đơn) và kiểm thử độc lập.
///
/// Lưu ý về VAT và cọc (giải quyết mâu thuẫn giữa ví dụ và công thức trong docs/screens/07-billing.md):
/// VAT áp trên (phòng + dịch vụ + phụ thu − giảm giá), KHÔNG trừ cọc; cọc chỉ đối trừ ở bước cuối
/// để ra số dư phải thu — khớp các con số ví dụ ở SCR-F02 và luồng SCR-F05.
/// </summary>
public interface IPricingService
{
    /// <summary>Số đêm = số ngày lịch giữa ngày đến và ngày đi, tối thiểu 1 — BR-02.</summary>
    int CountNights(DateTime checkIn, DateTime checkOut);

    decimal RoomCharge(decimal pricePerNight, int nights);

    /// <summary>Phụ thu nhận phòng sớm theo giờ nhận thực tế; null nếu nhận đúng giờ chuẩn trở đi — BR-03.</summary>
    SurchargeLine? EarlyCheckInSurcharge(TimeOnly actualCheckIn, decimal pricePerNight, PricingSettings s);

    /// <summary>Phụ thu trả phòng trễ; sau mốc cấu hình thì tính thêm 1 đêm; null nếu trả đúng giờ chuẩn trở về trước — BR-03.</summary>
    SurchargeLine? LateCheckOutSurcharge(TimeOnly actualCheckOut, decimal pricePerNight, PricingSettings s);

    /// <summary>Phụ thu thêm người cho số đêm áp dụng khi vượt sức chứa chuẩn — BR-03.</summary>
    SurchargeLine? ExtraGuestSurcharge(int totalGuests, int standardCapacity, decimal feePerGuestPerNight, int nights);

    FolioTotal ComputeFolioTotal(
        decimal roomCharge, decimal serviceCharge, decimal surchargeAmount,
        decimal discountAmount, decimal depositApplied, PricingSettings s);

    /// <summary>Phí hủy theo thời điểm hủy so với ngày đến — BR-05.</summary>
    decimal CancellationFee(decimal depositHeld, double hoursBeforeArrival, PricingSettings s);

    /// <summary>Làm tròn tới đơn vị cấu hình (mặc định 1.000 ₫) — BR-04.</summary>
    decimal RoundToUnit(decimal amount, int unit);
}

/// <inheritdoc />
public class PricingService : IPricingService
{
    public int CountNights(DateTime checkIn, DateTime checkOut)
    {
        var nights = (checkOut.Date - checkIn.Date).Days;
        return nights < 1 ? 1 : nights;
    }

    public decimal RoomCharge(decimal pricePerNight, int nights)
        => pricePerNight * nights;

    public SurchargeLine? EarlyCheckInSurcharge(TimeOnly actualCheckIn, decimal pricePerNight, PricingSettings s)
    {
        // Nhận đúng giờ chuẩn hoặc muộn hơn thì không có phụ thu nhận sớm.
        if (actualCheckIn >= s.StandardCheckInTime)
        {
            return null;
        }

        var boundary = new TimeOnly(s.EarlyCheckInBoundaryHour, 0);
        var (rate, label) = actualCheckIn < boundary
            ? (s.EarlyCheckInBeforeBoundaryRate, $"Nhận phòng sớm trước {boundary:HH\\:mm}")
            : (s.EarlyCheckInToStandardRate, $"Nhận phòng sớm {boundary:HH\\:mm}–{s.StandardCheckInTime:HH\\:mm}");

        return new SurchargeLine
        {
            Type = SurchargeType.EarlyCheckIn,
            Amount = pricePerNight * rate,
            Description = $"{label} ({rate:P0} giá đêm)"
        };
    }

    public SurchargeLine? LateCheckOutSurcharge(TimeOnly actualCheckOut, decimal pricePerNight, PricingSettings s)
    {
        // Trả đúng giờ chuẩn hoặc sớm hơn thì không có phụ thu.
        if (actualCheckOut <= s.StandardCheckOutTime)
        {
            return null;
        }

        // Tính số giờ trả trễ (làm tròn lên — ví dụ: 12:01 → 1 giờ, 13:30 → 2 giờ).
        var lateMinutes = (actualCheckOut - s.StandardCheckOutTime).TotalMinutes;
        var hoursLate = (int)Math.Ceiling(lateMinutes / 60.0);

        const decimal RatePerHour = 200_000m;
        var amount = RatePerHour * hoursLate;

        return new SurchargeLine
        {
            Type = SurchargeType.LateCheckOut,
            Amount = amount,
            Description = $"Trả trễ {hoursLate} giờ × 200.000 ₫/giờ (sau {s.StandardCheckOutTime:HH\\:mm})"
        };
    }


    public SurchargeLine? ExtraGuestSurcharge(int totalGuests, int standardCapacity, decimal feePerGuestPerNight, int nights)
    {
        var extra = totalGuests - standardCapacity;
        if (extra <= 0 || feePerGuestPerNight <= 0 || nights <= 0)
        {
            return null;
        }

        return new SurchargeLine
        {
            Type = SurchargeType.ExtraGuest,
            Amount = feePerGuestPerNight * extra * nights,
            Description = $"Thêm {extra} người × {nights} đêm"
        };
    }

    public FolioTotal ComputeFolioTotal(
        decimal roomCharge, decimal serviceCharge, decimal surchargeAmount,
        decimal discountAmount, decimal depositApplied, PricingSettings s)
    {
        var subTotal = roomCharge + serviceCharge + surchargeAmount - discountAmount;
        var tax = subTotal * s.VatRate;
        var total = RoundToUnit(subTotal + tax, s.RoundingUnit);

        return new FolioTotal
        {
            RoomCharge = roomCharge,
            ServiceCharge = serviceCharge,
            SurchargeAmount = surchargeAmount,
            DiscountAmount = discountAmount,
            SubTotal = subTotal,
            TaxRate = s.VatRate,
            TaxAmount = tax,
            Total = total,
            DepositApplied = depositApplied,
            BalanceDue = total - depositApplied
        };
    }

    public decimal CancellationFee(decimal depositHeld, double hoursBeforeArrival, PricingSettings s)
    {
        if (depositHeld <= 0)
        {
            return 0m;
        }

        // BR-05: càng sát ngày đến, giữ lại càng nhiều cọc.
        var rate = hoursBeforeArrival >= 48
            ? s.CancelFeeOver48hRate
            : hoursBeforeArrival >= 24
                ? s.CancelFee24To48hRate
                : s.CancelFeeUnder24hRate;

        return depositHeld * rate;
    }

    public decimal RoundToUnit(decimal amount, int unit)
    {
        if (unit <= 0)
        {
            return amount;
        }

        return Math.Round(amount / unit, MidpointRounding.AwayFromZero) * unit;
    }
}
