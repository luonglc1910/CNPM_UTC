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

    /// <summary>Giờ mở gói qua đêm — BR-13.</summary>
    public int OvernightStartHour { get; init; } = 22;

    /// <summary>Giờ kết thúc gói qua đêm, sáng hôm sau — BR-13.</summary>
    public int OvernightEndHour { get; init; } = 10;

    /// <summary>Phút lẻ được bỏ qua khi tính giờ; lẻ quá mức này mới lên một giờ — BR-13.</summary>
    public int HourlyGraceMinutes { get; init; } = 20;

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

/// <summary>
/// Kết quả quy đổi khoảng thời gian ở thành số giờ tính tiền — BR-13.
/// Giữ lại phần phút lẻ và cờ đã-làm-tròn-lên để màn hình trả phòng giải thích được với khách
/// vì sao hóa đơn ghi 3 giờ trong khi khách chỉ ở 2 giờ 35 phút.
/// </summary>
/// <param name="Hours">Số giờ đưa vào hóa đơn, tối thiểu 1.</param>
/// <param name="TotalMinutes">Tổng số phút ở thực tế.</param>
/// <param name="OddMinutes">Số phút lẻ ngoài các giờ tròn.</param>
/// <param name="RoundedUp">Phần lẻ đã vượt mức bỏ qua nên bị tính thêm một giờ.</param>
public record HourCount(int Hours, int TotalMinutes, int OddMinutes, bool RoundedUp)
{
    /// <summary>Khoảng thời gian ở dạng "2 giờ 35 phút", để ghép vào câu giải thích.</summary>
    public string SpanText
    {
        get
        {
            var h = TotalMinutes / 60;
            var m = TotalMinutes % 60;
            if (h == 0)
            {
                return $"{m} phút";
            }

            return m == 0 ? $"{h} giờ" : $"{h} giờ {m} phút";
        }
    }
}

/// <summary>
/// Khoảng thời gian thuê đã chuẩn hóa theo hình thức — BR-13.
///
/// Người dùng nhập mỗi hình thức một kiểu (ngày, ngày+giờ, hoặc chỉ ngày của đêm), còn phần
/// còn lại của hệ thống chỉ muốn biết hai mốc <see cref="CheckIn"/>/<see cref="CheckOut"/> đầy đủ.
/// Quy đổi tập trung ở một chỗ để tra phòng trống, sơ đồ phòng và tính tiền không lệch nhau.
/// </summary>
/// <param name="Nights">Số đêm; thuê theo giờ là 0, qua đêm luôn là 1.</param>
/// <param name="Hours">Số giờ dự kiến; chỉ thuê theo giờ mới khác 0.</param>
public record RentalPeriod(RentalType Type, DateTime CheckIn, DateTime CheckOut, int Nights, int Hours)
{
    /// <summary>Mô tả khoảng thuê để in lên dòng tiền phòng và màn hình chi tiết.</summary>
    public string SpanText => Type switch
    {
        RentalType.Hourly => $"từ {CheckIn:HH\\:mm dd/MM}, tính giờ khi trả phòng",
        RentalType.Overnight => $"qua đêm {CheckIn:HH\\:mm dd/MM} → {CheckOut:HH\\:mm dd/MM}",
        _ => $"{Nights} đêm"
    };
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

    /// <summary>
    /// Quy đổi khoảng thời gian ở thành số giờ tính tiền — BR-13.
    /// Phần lẻ từ <see cref="PricingSettings.HourlyGraceMinutes"/> phút trở xuống thì bỏ,
    /// quá mức đó mới lên một giờ. Ở dưới một giờ vẫn tính tròn một giờ.
    /// </summary>
    HourCount CountHours(DateTime from, DateTime to, PricingSettings s);

    /// <summary>Tiền phòng thuê theo giờ: giờ đầu một giá, mỗi giờ tiếp theo một giá — BR-13.</summary>
    decimal HourlyRoomCharge(decimal priceFirstHour, decimal priceExtraHour, int hours);

    /// <summary>
    /// Khung giờ của một gói qua đêm tính từ ngày mở gói — BR-13.
    /// Mặc định 22:00 ngày đó đến 10:00 hôm sau; hai mốc lấy từ cấu hình.
    /// </summary>
    (DateTime Start, DateTime End) OvernightWindow(DateTime night, PricingSettings s);

    /// <summary>
    /// Chuẩn hóa cặp thời điểm người dùng nhập thành khoảng thuê đầy đủ — BR-13.
    /// Trả về câu lỗi tiếng Việt thay vì ném ngoại lệ, để tầng trên gắn thẳng vào ModelState.
    /// </summary>
    (RentalPeriod? Period, string? Error) ResolvePeriod(
        RentalType type, DateTime rawCheckIn, DateTime rawCheckOut, PricingSettings s);

    /// <summary>Tiền phòng của cả khoảng thuê, chọn đúng bảng giá theo hình thức — BR-13.</summary>
    decimal RoomChargeFor(RentalPeriod period, decimal pricePerNight,
        decimal priceFirstHour, decimal priceExtraHour, decimal priceOvernight);

    /// <summary>
    /// Phụ thu ở quá giờ kết thúc gói qua đêm, tính theo giờ với cùng luật làm tròn — BR-13.
    /// Trả null nếu trả phòng đúng giờ hoặc sớm hơn.
    /// </summary>
    SurchargeLine? OvernightOverstaySurcharge(DateTime packageEnd, DateTime actualCheckOut, decimal priceExtraHour, PricingSettings s);

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

    public HourCount CountHours(DateTime from, DateTime to, PricingSettings s)
    {
        var totalMinutes = (int)Math.Round((to - from).TotalMinutes, MidpointRounding.AwayFromZero);
        if (totalMinutes < 0)
        {
            totalMinutes = 0;
        }

        var whole = totalMinutes / 60;
        var odd = totalMinutes % 60;

        // Lẻ đúng bằng mức bỏ qua thì vẫn bỏ — "dưới 20 phút làm tròn xuống" hiểu theo nghĩa
        // 20 phút chưa phải là "quá 20 phút".
        var grace = s.HourlyGraceMinutes < 0 ? 0 : s.HourlyGraceMinutes;
        var hours = odd > grace ? whole + 1 : whole;

        var roundedUp = hours > whole;

        // Ghé chưa đầy một giờ vẫn phải trả tiền giờ đầu, nhưng đó không phải là làm tròn lên
        // theo nghĩa của cờ này — không có gì để giải thích với khách.
        if (hours < 1)
        {
            hours = 1;
        }

        return new HourCount(hours, totalMinutes, odd, roundedUp);
    }

    public decimal HourlyRoomCharge(decimal priceFirstHour, decimal priceExtraHour, int hours)
    {
        if (hours < 1)
        {
            hours = 1;
        }

        return priceFirstHour + priceExtraHour * (hours - 1);
    }

    public (DateTime Start, DateTime End) OvernightWindow(DateTime night, PricingSettings s)
    {
        var start = night.Date.AddHours(s.OvernightStartHour);
        var end = night.Date.AddDays(1).AddHours(s.OvernightEndHour);
        return (start, end);
    }

    public (RentalPeriod? Period, string? Error) ResolvePeriod(
        RentalType type, DateTime rawCheckIn, DateTime rawCheckOut, PricingSettings s)
    {
        switch (type)
        {
            case RentalType.Hourly:
            {
                // Bỏ phần giây: ô datetime-local chỉ cho tới phút, giữ giây chỉ làm hóa đơn
                // lệch một giờ ở sát ngưỡng làm tròn mà không ai giải thích được.
                var checkIn = TrimToMinute(rawCheckIn);

                // Thuê theo giờ không có giờ đi: khách ở bao lâu thì lúc trả phòng mới biết, và đó
                // chính là điểm khác của hình thức này. Giá trị lưu ở đây chỉ là mốc tạm một giờ —
                // đúng bằng mức tối thiểu phải trả — để phần chống trùng lịch và lưới tình trạng
                // phòng vẫn có hai đầu mà so sánh. Số thật được chốt ở màn trả phòng.
                var provisionalEnd = checkIn.AddHours(1);

                return (new RentalPeriod(type, checkIn, provisionalEnd, Nights: 0, Hours: 1), null);
            }

            case RentalType.Overnight:
            {
                // Chỉ phần ngày của ô "đêm ngày" có nghĩa; hai mốc giờ do cấu hình quyết định.
                var (start, end) = OvernightWindow(rawCheckIn, s);
                return (new RentalPeriod(type, start, end, Nights: 1, Hours: 0), null);
            }

            default:
            {
                if (rawCheckOut.Date <= rawCheckIn.Date)
                {
                    return (null, "Ngày đi phải sau ngày đến.");
                }

                // Gắn giờ chuẩn vào hai mốc thay vì để 00:00 — nếu không, một lượt thuê theo giờ
                // buổi sáng sẽ bị coi là đụng lịch với đơn trả phòng trưa hôm đó.
                var checkIn = rawCheckIn.Date.Add(s.StandardCheckInTime.ToTimeSpan());
                var checkOut = rawCheckOut.Date.Add(s.StandardCheckOutTime.ToTimeSpan());
                var nights = CountNights(rawCheckIn, rawCheckOut);

                return (new RentalPeriod(RentalType.Daily, checkIn, checkOut, nights, Hours: 0), null);
            }
        }
    }

    public decimal RoomChargeFor(RentalPeriod period, decimal pricePerNight,
        decimal priceFirstHour, decimal priceExtraHour, decimal priceOvernight)
        => period.Type switch
        {
            RentalType.Hourly => HourlyRoomCharge(priceFirstHour, priceExtraHour, period.Hours),
            RentalType.Overnight => priceOvernight,
            _ => RoomCharge(pricePerNight, period.Nights)
        };

    private static DateTime TrimToMinute(DateTime value)
        => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);

    public SurchargeLine? OvernightOverstaySurcharge(
        DateTime packageEnd, DateTime actualCheckOut, decimal priceExtraHour, PricingSettings s)
    {
        if (actualCheckOut <= packageEnd)
        {
            return null;
        }

        var over = CountHours(packageEnd, actualCheckOut, s);
        var amount = priceExtraHour * over.Hours;

        return new SurchargeLine
        {
            Type = SurchargeType.OvernightOverstay,
            Amount = amount,
            Description = $"Quá gói qua đêm {over.SpanText} (sau {packageEnd:HH\\:mm}) — tính {over.Hours} giờ × {priceExtraHour:N0} ₫"
        };
    }

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
