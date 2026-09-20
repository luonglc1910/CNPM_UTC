using System.Globalization;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Đọc từng tham số cấu hình theo kiểu, dùng cho tầng nghiệp vụ (tính giá, phòng trống, thu ngân).
///
/// Khác <see cref="ISettingsService"/> vốn dựng cả <c>SettingsViewModel</c> cho màn hình cấu hình
/// và quy đổi tỷ lệ sang phần trăm: reader này trả về **giá trị thô** (tỷ lệ 0.08, không phải 8)
/// và chỉ nạp bảng SystemSettings **một lần cho mỗi request** rồi dùng lại, tránh mỗi phép tính
/// lại truy vấn DB. Thiếu khóa thì lùi về giá trị mặc định ở <see cref="SystemSettingDefaults"/>.
/// </summary>
public interface ISettingsReader
{
    Task<string> GetStringAsync(string key);
    Task<decimal> GetDecimalAsync(string key);
    Task<int> GetIntAsync(string key);
    Task<bool> GetBoolAsync(string key);
    Task<TimeOnly> GetTimeAsync(string key);

    /// <summary>Gom mọi tham số phục vụ tính tiền/phụ thu/hủy vào một ảnh chụp để truyền cho <see cref="IPricingService"/>.</summary>
    Task<PricingSettings> GetPricingSettingsAsync();
}

/// <inheritdoc />
public class SettingsReader : ISettingsReader
{
    private readonly HotelDbContext _db;
    private Dictionary<string, string>? _cache;

    public SettingsReader(HotelDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetStringAsync(string key)
    {
        var values = await EnsureLoadedAsync();
        return values.TryGetValue(key, out var value)
            ? value
            : DefaultOf(key);
    }

    public async Task<decimal> GetDecimalAsync(string key)
        => ParseDecimal(await GetStringAsync(key));

    public async Task<int> GetIntAsync(string key)
        => int.TryParse(await GetStringAsync(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

    public async Task<bool> GetBoolAsync(string key)
        => bool.TryParse(await GetStringAsync(key), out var b) && b;

    public async Task<TimeOnly> GetTimeAsync(string key)
        => TimeOnly.TryParse(await GetStringAsync(key), CultureInfo.InvariantCulture, out var t)
            ? t
            : new TimeOnly(0, 0);

    public async Task<PricingSettings> GetPricingSettingsAsync()
    {
        await EnsureLoadedAsync();

        return new PricingSettings
        {
            VatRate = await GetDecimalAsync(SystemSettingKeys.VatRate),
            RoundingUnit = await GetIntAsync(SystemSettingKeys.RoundingUnit),
            StandardCheckInTime = await GetTimeAsync(SystemSettingKeys.StandardCheckInTime),
            StandardCheckOutTime = await GetTimeAsync(SystemSettingKeys.StandardCheckOutTime),
            EarlyCheckInBoundaryHour = await GetIntAsync(SystemSettingKeys.EarlyCheckInBoundaryHour),
            EarlyCheckInBeforeBoundaryRate = await GetDecimalAsync(SystemSettingKeys.EarlyCheckInBefore09Rate),
            EarlyCheckInToStandardRate = await GetDecimalAsync(SystemSettingKeys.EarlyCheckIn09To14Rate),
            LateCheckOutTier1EndHour = await GetIntAsync(SystemSettingKeys.LateCheckOutTier1EndHour),
            LateCheckOutTier1Rate = await GetDecimalAsync(SystemSettingKeys.LateCheckOut12To15Rate),
            LateCheckOutTier2Rate = await GetDecimalAsync(SystemSettingKeys.LateCheckOut15To18Rate),
            LateCheckOutFullNightHour = await GetIntAsync(SystemSettingKeys.LateCheckOutFullNightHour),
            OvernightStartHour = await GetIntAsync(SystemSettingKeys.OvernightStartHour),
            OvernightEndHour = await GetIntAsync(SystemSettingKeys.OvernightEndHour),
            HourlyGraceMinutes = await GetIntAsync(SystemSettingKeys.HourlyGraceMinutes),
            DepositNights = await GetIntAsync(SystemSettingKeys.DepositNights),
            HoldUntilHour = await GetIntAsync(SystemSettingKeys.HoldUntilHour),
            CancelFeeOver48hRate = await GetDecimalAsync(SystemSettingKeys.CancelFeeOver48hRate),
            CancelFee24To48hRate = await GetDecimalAsync(SystemSettingKeys.CancelFee24To48hRate),
            CancelFeeUnder24hRate = await GetDecimalAsync(SystemSettingKeys.CancelFeeUnder24hRate),
            ChildAgeLimit = await GetIntAsync(SystemSettingKeys.ChildAgeLimit)
        };
    }

    private async Task<Dictionary<string, string>> EnsureLoadedAsync()
        => _cache ??= await _db.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.Ordinal);

    private static string DefaultOf(string key)
        => SystemSettingDefaults.All.FirstOrDefault(d => d.Key == key).Value ?? string.Empty;

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
