using System.Globalization;
using HotelManagement.Web.Data;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Services;

/// <summary>
/// Đọc/ghi tham số cấu hình — SCR-A12, FR-A07, BR-11.
///
/// Giá trị lưu dạng chuỗi trong bảng SystemSettings nên mọi chuyển đổi kiểu (tỷ lệ ↔ phần trăm,
/// chuỗi ↔ số) đều gom về đây; controller và view chỉ làm việc với kiểu đã đúng.
/// Mọi thay đổi đều ghi nhật ký giá trị cũ → mới, ghi theo **từng khóa** để sau này tra ra
/// đúng tham số nào đổi chứ không phải cả nhóm.
/// </summary>
public interface ISettingsService
{
    Task<SettingsViewModel> GetAsync();

    Task<ServiceResult> SaveHotelAsync(HotelInfoSettings form, string? newLogoPath, bool removeLogo);
    Task<ServiceResult> SaveCheckInOutAsync(CheckInOutSettings form);
    Task<ServiceResult> SaveTaxAsync(TaxSettings form);
    Task<ServiceResult> SaveSurchargeAsync(SurchargeSettings form);
    Task<ServiceResult> SaveCancellationAsync(CancellationSettings form);
    Task<ServiceResult> SaveLimitAsync(LimitSettings form);

    /// <summary>Đưa cả nhóm về giá trị mặc định — SCR-A12.</summary>
    Task<ServiceResult> ResetGroupAsync(string group);

    /// <summary>Đường dẫn logo hiện tại, để xóa file cũ khi thay logo mới.</summary>
    Task<string?> GetLogoPathAsync();
}

/// <inheritdoc />
public class SettingsService : ISettingsService
{
    private readonly HotelDbContext _db;
    private readonly IAuditService _audit;

    public SettingsService(HotelDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<SettingsViewModel> GetAsync()
    {
        var values = await LoadAsync();

        return new SettingsViewModel
        {
            Hotel = new HotelInfoSettings
            {
                Name = Text(values, SystemSettingKeys.HotelName),
                Address = Text(values, SystemSettingKeys.HotelAddress),
                Phone = Text(values, SystemSettingKeys.HotelPhone),
                TaxCode = Text(values, SystemSettingKeys.HotelTaxCode),
                LogoPath = NullIfEmpty(Text(values, SystemSettingKeys.HotelLogoPath))
            },
            CheckInOut = new CheckInOutSettings
            {
                StandardCheckInTime = Text(values, SystemSettingKeys.StandardCheckInTime),
                StandardCheckOutTime = Text(values, SystemSettingKeys.StandardCheckOutTime),
                OvernightStartHour = Int(values, SystemSettingKeys.OvernightStartHour),
                OvernightEndHour = Int(values, SystemSettingKeys.OvernightEndHour)
            },
            Tax = new TaxSettings
            {
                VatPercent = Percent(values, SystemSettingKeys.VatRate),
                RoundingUnit = Int(values, SystemSettingKeys.RoundingUnit)
            },
            Surcharge = new SurchargeSettings
            {
                EarlyCheckInBoundaryHour = Int(values, SystemSettingKeys.EarlyCheckInBoundaryHour),
                EarlyCheckInBeforeBoundaryPercent = Percent(values, SystemSettingKeys.EarlyCheckInBefore09Rate),
                EarlyCheckInToStandardPercent = Percent(values, SystemSettingKeys.EarlyCheckIn09To14Rate),
                LateCheckOutTier1EndHour = Int(values, SystemSettingKeys.LateCheckOutTier1EndHour),
                LateCheckOutTier1Percent = Percent(values, SystemSettingKeys.LateCheckOut12To15Rate),
                LateCheckOutFullNightHour = Int(values, SystemSettingKeys.LateCheckOutFullNightHour),
                LateCheckOutTier2Percent = Percent(values, SystemSettingKeys.LateCheckOut15To18Rate),
                HourlyGraceMinutes = Int(values, SystemSettingKeys.HourlyGraceMinutes)
            },
            Cancellation = new CancellationSettings
            {
                DepositNights = Int(values, SystemSettingKeys.DepositNights),
                HoldUntilHour = Int(values, SystemSettingKeys.HoldUntilHour),
                Over48hPercent = Percent(values, SystemSettingKeys.CancelFeeOver48hRate),
                Between24And48hPercent = Percent(values, SystemSettingKeys.CancelFee24To48hRate),
                Under24hPercent = Percent(values, SystemSettingKeys.CancelFeeUnder24hRate)
            },
            Limit = new LimitSettings
            {
                ReceptionistMaxDiscountAmount = Decimal(values, SystemSettingKeys.ReceptionistMaxDiscountAmount),
                ReceptionistMaxDiscountPercent = Percent(values, SystemSettingKeys.ReceptionistMaxDiscountRate),
                AllowSellWhenOutOfStock = Bool(values, SystemSettingKeys.AllowSellWhenOutOfStock),
                LoyalGuestStayThreshold = Int(values, SystemSettingKeys.LoyalGuestStayThreshold),
                ChildAgeLimit = Int(values, SystemSettingKeys.ChildAgeLimit)
            }
        };
    }

    public async Task<string?> GetLogoPathAsync()
    {
        var setting = await _db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SystemSettingKeys.HotelLogoPath);

        return NullIfEmpty(setting?.Value ?? string.Empty);
    }

    // ---------- Lưu từng nhóm ----------

    public async Task<ServiceResult> SaveHotelAsync(HotelInfoSettings form, string? newLogoPath, bool removeLogo)
    {
        var updates = new Dictionary<string, string>
        {
            [SystemSettingKeys.HotelName] = form.Name.Trim(),
            [SystemSettingKeys.HotelAddress] = form.Address.Trim(),
            [SystemSettingKeys.HotelPhone] = form.Phone.Trim(),
            [SystemSettingKeys.HotelTaxCode] = form.TaxCode?.Trim() ?? string.Empty
        };

        // Không gửi file mới và không bấm xóa thì giữ nguyên logo cũ.
        if (newLogoPath is not null)
        {
            updates[SystemSettingKeys.HotelLogoPath] = newLogoPath;
        }
        else if (removeLogo)
        {
            updates[SystemSettingKeys.HotelLogoPath] = string.Empty;
        }

        return await ApplyAsync(SystemSettingDefaults.Groups.Hotel, updates);
    }

    public async Task<ServiceResult> SaveCheckInOutAsync(CheckInOutSettings form)
    {
        // Gói qua đêm bắt buộc vắt qua nửa đêm — BR-13. Giờ đóng bằng hoặc muộn hơn giờ mở thì
        // gói dài 26 tiếng hoặc âm, và mọi phép tính phụ thu quá giờ sau đó đều vô nghĩa.
        if (form.OvernightEndHour >= form.OvernightStartHour)
        {
            return ServiceResult.Fail(
                "Giờ kết thúc gói qua đêm phải sớm hơn giờ mở gói — gói chạy sang ngày hôm sau.",
                nameof(form.OvernightEndHour));
        }

        return await ApplyAsync(SystemSettingDefaults.Groups.CheckInOut, new Dictionary<string, string>
        {
            [SystemSettingKeys.StandardCheckInTime] = form.StandardCheckInTime.Trim(),
            [SystemSettingKeys.StandardCheckOutTime] = form.StandardCheckOutTime.Trim(),
            [SystemSettingKeys.OvernightStartHour] = form.OvernightStartHour.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.OvernightEndHour] = form.OvernightEndHour.ToString(CultureInfo.InvariantCulture)
        });
    }

    public async Task<ServiceResult> SaveTaxAsync(TaxSettings form)
        => await ApplyAsync(SystemSettingDefaults.Groups.Tax, new Dictionary<string, string>
        {
            [SystemSettingKeys.VatRate] = FromPercent(form.VatPercent),
            [SystemSettingKeys.RoundingUnit] = form.RoundingUnit.ToString(CultureInfo.InvariantCulture)
        });

    public async Task<ServiceResult> SaveSurchargeAsync(SurchargeSettings form)
    {
        // Ràng buộc liên trường: ba mốc giờ trả phòng phải tăng dần, nếu không thì có khoảng giờ
        // không rơi vào bậc nào, hoặc rơi vào hai bậc cùng lúc.
        if (form.LateCheckOutFullNightHour <= form.LateCheckOutTier1EndHour)
        {
            return ServiceResult.Fail(
                "Mốc giờ tính thêm 1 đêm phải muộn hơn mốc kết thúc bậc 1.",
                nameof(form.LateCheckOutFullNightHour));
        }

        return await ApplyAsync(SystemSettingDefaults.Groups.Surcharge, new Dictionary<string, string>
        {
            [SystemSettingKeys.EarlyCheckInBoundaryHour] = form.EarlyCheckInBoundaryHour.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.EarlyCheckInBefore09Rate] = FromPercent(form.EarlyCheckInBeforeBoundaryPercent),
            [SystemSettingKeys.EarlyCheckIn09To14Rate] = FromPercent(form.EarlyCheckInToStandardPercent),
            [SystemSettingKeys.LateCheckOutTier1EndHour] = form.LateCheckOutTier1EndHour.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.LateCheckOut12To15Rate] = FromPercent(form.LateCheckOutTier1Percent),
            [SystemSettingKeys.LateCheckOutFullNightHour] = form.LateCheckOutFullNightHour.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.LateCheckOut15To18Rate] = FromPercent(form.LateCheckOutTier2Percent),
            [SystemSettingKeys.HourlyGraceMinutes] = form.HourlyGraceMinutes.ToString(CultureInfo.InvariantCulture)
        });
    }

    public async Task<ServiceResult> SaveCancellationAsync(CancellationSettings form)
        => await ApplyAsync(SystemSettingDefaults.Groups.Cancellation, new Dictionary<string, string>
        {
            [SystemSettingKeys.DepositNights] = form.DepositNights.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.HoldUntilHour] = form.HoldUntilHour.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.CancelFeeOver48hRate] = FromPercent(form.Over48hPercent),
            [SystemSettingKeys.CancelFee24To48hRate] = FromPercent(form.Between24And48hPercent),
            [SystemSettingKeys.CancelFeeUnder24hRate] = FromPercent(form.Under24hPercent)
        });

    public async Task<ServiceResult> SaveLimitAsync(LimitSettings form)
        => await ApplyAsync(SystemSettingDefaults.Groups.Limit, new Dictionary<string, string>
        {
            [SystemSettingKeys.ReceptionistMaxDiscountAmount] = form.ReceptionistMaxDiscountAmount.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.ReceptionistMaxDiscountRate] = FromPercent(form.ReceptionistMaxDiscountPercent),
            [SystemSettingKeys.AllowSellWhenOutOfStock] = form.AllowSellWhenOutOfStock ? "true" : "false",
            [SystemSettingKeys.LoyalGuestStayThreshold] = form.LoyalGuestStayThreshold.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.ChildAgeLimit] = form.ChildAgeLimit.ToString(CultureInfo.InvariantCulture)
        });

    public async Task<ServiceResult> ResetGroupAsync(string group)
    {
        if (!SystemSettingDefaults.Groups.All.Contains(group, StringComparer.Ordinal))
        {
            return ServiceResult.Fail("Nhóm cấu hình không hợp lệ.");
        }

        var defaults = SystemSettingDefaults.All
            .Where(d => d.Group == group)
            .ToDictionary(d => d.Key, d => d.Value, StringComparer.Ordinal);

        var result = await ApplyAsync(group, defaults, isReset: true);
        if (!result.Succeeded)
        {
            return result;
        }

        return ServiceResult.Ok(message:
            $"Đã khôi phục mặc định nhóm \"{SystemSettingDefaults.GroupDisplayName(group)}\".");
    }

    /// <summary>
    /// Ghi các khóa của một nhóm. Chỉ khóa nào **thực sự đổi giá trị** mới sinh dòng nhật ký —
    /// bấm Lưu mà không sửa gì thì không làm bẩn nhật ký.
    /// </summary>
    private async Task<ServiceResult> ApplyAsync(
        string group, IReadOnlyDictionary<string, string> updates, bool isReset = false)
    {
        var entities = await _db.SystemSettings
            .Where(s => updates.Keys.Contains(s.Key))
            .ToListAsync();

        var changed = 0;

        foreach (var (key, value) in updates)
        {
            var entity = entities.FirstOrDefault(e => e.Key == key);

            if (entity is null)
            {
                // Khóa có trong bảng mặc định mà chưa có trong DB (DB cũ hơn bản cài) — tạo mới.
                var definition = SystemSettingDefaults.All.FirstOrDefault(d => d.Key == key);
                _db.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Group = definition.Group ?? group,
                    Description = definition.Description
                });

                _audit.Log("ChangeSetting", nameof(SystemSetting), key, newValue: value);
                changed++;
                continue;
            }

            if (string.Equals(entity.Value, value, StringComparison.Ordinal))
            {
                continue;
            }

            // BR-11: nhật ký giữ nguyên giá trị cũ để còn đối chiếu về sau.
            _audit.Log(isReset ? "ResetSetting" : "ChangeSetting", nameof(SystemSetting), key,
                oldValue: entity.Value, newValue: value);

            entity.Value = value;
            changed++;
        }

        if (changed == 0)
        {
            return ServiceResult.Ok(message:
                $"Nhóm \"{SystemSettingDefaults.GroupDisplayName(group)}\" không có thay đổi nào.");
        }

        await _db.SaveChangesAsync();

        return ServiceResult.Ok(message:
            $"Đã lưu {changed} tham số của nhóm \"{SystemSettingDefaults.GroupDisplayName(group)}\".");
    }

    private async Task<Dictionary<string, string>> LoadAsync()
        => await _db.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, StringComparer.Ordinal);

    // ---------- Chuyển đổi kiểu ----------

    /// <summary>Thiếu khóa thì lùi về giá trị mặc định thay vì ném lỗi — màn hình vẫn mở được.</summary>
    private static string Text(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value)
            ? value
            : SystemSettingDefaults.All.FirstOrDefault(d => d.Key == key).Value ?? string.Empty;

    private static decimal Decimal(IReadOnlyDictionary<string, string> values, string key)
        => decimal.TryParse(Text(values, key), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static int Int(IReadOnlyDictionary<string, string> values, string key)
        => int.TryParse(Text(values, key), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private static bool Bool(IReadOnlyDictionary<string, string> values, string key)
        => bool.TryParse(Text(values, key), out var b) && b;

    /// <summary>
    /// DB lưu tỷ lệ (0.08), giao diện hiển thị phần trăm (8) — người dùng nghĩ theo %.
    /// Đọc lại qua chuỗi "0.##" để bỏ phần thập phân thừa: 0.08 × 100 ra 8.00, ô nhập hiện "8"
    /// mới đúng cái người dùng vừa gõ.
    /// </summary>
    private static decimal Percent(IReadOnlyDictionary<string, string> values, string key)
    {
        var rounded = Math.Round(Decimal(values, key) * 100m, 2);
        return decimal.Parse(rounded.ToString("0.##", CultureInfo.InvariantCulture),
            NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    private static string FromPercent(decimal percent)
        => (percent / 100m).ToString("0.####", CultureInfo.InvariantCulture);

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
