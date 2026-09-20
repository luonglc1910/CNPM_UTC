using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Tham số cấu hình hệ thống — FR-A07. Lưu dạng khóa-giá trị để thêm tham số mới
/// không phải tạo migration. Danh sách khóa chuẩn nằm ở SystemSettingKeys.
/// </summary>
public class SystemSetting : BaseEntity
{
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>Nhóm hiển thị trên màn hình cấu hình: Hotel, CheckInOut, Tax, Surcharge, Cancellation, Limit.</summary>
    [MaxLength(50)]
    public string Group { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

/// <summary>Khóa cấu hình chuẩn — tránh gõ chuỗi tự do rải rác trong code.</summary>
public static class SystemSettingKeys
{
    // Thông tin khách sạn
    public const string HotelName = "Hotel.Name";
    public const string HotelAddress = "Hotel.Address";
    public const string HotelPhone = "Hotel.Phone";
    public const string HotelTaxCode = "Hotel.TaxCode";

    /// <summary>Đường dẫn tương đối tới file logo trong wwwroot, rỗng = chưa có logo.</summary>
    public const string HotelLogoPath = "Hotel.LogoPath";

    // Giờ chuẩn — BR-01
    public const string StandardCheckInTime = "CheckInOut.StandardCheckInTime";
    public const string StandardCheckOutTime = "CheckInOut.StandardCheckOutTime";

    // Thuế và làm tròn — BR-04
    public const string VatRate = "Tax.VatRate";
    public const string RoundingUnit = "Tax.RoundingUnit";

    // Phụ thu — BR-03
    public const string EarlyCheckInBefore09Rate = "Surcharge.EarlyCheckInBefore09Rate";
    public const string EarlyCheckIn09To14Rate = "Surcharge.EarlyCheckIn09To14Rate";
    public const string LateCheckOut12To15Rate = "Surcharge.LateCheckOut12To15Rate";
    public const string LateCheckOut15To18Rate = "Surcharge.LateCheckOut15To18Rate";

    /// <summary>Mốc giờ chia hai bậc nhận phòng sớm — mặc định 09:00.</summary>
    public const string EarlyCheckInBoundaryHour = "Surcharge.EarlyCheckInBoundaryHour";

    /// <summary>Mốc giờ kết thúc bậc trả trễ thứ nhất — mặc định 15:00.</summary>
    public const string LateCheckOutTier1EndHour = "Surcharge.LateCheckOutTier1EndHour";

    /// <summary>Trả sau mốc này thì tính thêm hẳn một đêm — mặc định 18:00.</summary>
    public const string LateCheckOutFullNightHour = "Surcharge.LateCheckOutFullNightHour";

    // Cọc và hủy — BR-05
    public const string DepositNights = "Cancellation.DepositNights";
    public const string HoldUntilHour = "Cancellation.HoldUntilHour";
    public const string CancelFeeOver48hRate = "Cancellation.Over48hRate";
    public const string CancelFee24To48hRate = "Cancellation.Between24And48hRate";
    public const string CancelFeeUnder24hRate = "Cancellation.Under24hRate";

    // Hạn mức nghiệp vụ
    public const string ReceptionistMaxDiscountAmount = "Limit.ReceptionistMaxDiscountAmount";
    public const string ReceptionistMaxDiscountRate = "Limit.ReceptionistMaxDiscountRate";
    public const string LoyalGuestStayThreshold = "Limit.LoyalGuestStayThreshold";
    public const string ChildAgeLimit = "Limit.ChildAgeLimit";

    /// <summary>
    /// Ngưỡng chênh lệch tiền mặt khi đóng ca — SCR-F08. Vượt ngưỡng thì cảnh báo đỏ
    /// và ghi audit log mức cao.
    /// </summary>
    public const string CashDifferenceThreshold = "Limit.CashDifferenceThreshold";

    /// <summary>
    /// Công tắc tổng cho việc bán dịch vụ khi hết tồn — BR-12. Một dịch vụ chỉ bán âm được
    /// khi bật cả tham số này lẫn cờ riêng của dịch vụ đó (SCR-A07).
    /// </summary>
    public const string AllowSellWhenOutOfStock = "Limit.AllowSellWhenOutOfStock";
}
