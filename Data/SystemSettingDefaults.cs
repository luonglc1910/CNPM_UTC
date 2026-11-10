using HotelManagement.Web.Models.Entities;

namespace HotelManagement.Web.Data;

/// <summary>
/// Giá trị mặc định của mọi tham số cấu hình — FR-A07, BR-01 đến BR-05, BR-12.
///
/// Một nguồn sự thật duy nhất cho hai nơi: <see cref="DbInitializer"/> nạp lần đầu,
/// và nút "Khôi phục mặc định" từng nhóm ở SCR-A12. Để hai bảng riêng thì sớm muộn cũng lệch,
/// và lúc đó khôi phục mặc định lại ra giá trị khác với lúc cài mới.
/// </summary>
public static class SystemSettingDefaults
{
    /// <summary>Tên nhóm hiển thị trên màn hình cấu hình.</summary>
    public static class Groups
    {
        public const string Hotel = "Hotel";
        public const string CheckInOut = "CheckInOut";
        public const string Tax = "Tax";
        public const string Surcharge = "Surcharge";
        public const string Cancellation = "Cancellation";
        public const string Limit = "Limit";
        public const string Payment = "Payment";
        public const string DynamicPricing = "DynamicPricing";

        public static readonly string[] All = { Hotel, CheckInOut, Tax, Surcharge, Cancellation, Limit, Payment, DynamicPricing };
    }

    public const string DefaultSeasonalRulesJson = """
        [
          {"Id":"SR-1","Name":"Mùa Cao Điểm Hè","StartDate":"2026-06-01","EndDate":"2026-08-31","PercentChange":20,"RoomTypeIds":"","IsActive":true},
          {"Id":"SR-2","Name":"Dịp Lễ Quốc Khánh 2/9","StartDate":"2026-09-01","EndDate":"2026-09-04","PercentChange":30,"RoomTypeIds":"","IsActive":true},
          {"Id":"SR-3","Name":"Mùa Lễ Hội Cuối Năm & Tết","StartDate":"2026-12-20","EndDate":"2027-01-05","PercentChange":25,"RoomTypeIds":"","IsActive":true}
        ]
        """;

    public static readonly (string Key, string Value, string Group, string Description)[] All =
    {
        // 1. Thông tin khách sạn
        (SystemSettingKeys.HotelName, "AURA Boutique Hotel & Resort", Groups.Hotel, "Tên khách sạn in trên hóa đơn"),
        (SystemSettingKeys.HotelAddress, "54 Triều Khúc, Thanh Xuân, Hà Nội", Groups.Hotel, "Địa chỉ"),
        (SystemSettingKeys.HotelPhone, "0240 123 4567", Groups.Hotel, "Điện thoại"),
        (SystemSettingKeys.HotelTaxCode, "0100000000", Groups.Hotel, "Mã số thuế"),
        (SystemSettingKeys.HotelLogoPath, "", Groups.Hotel, "Logo in trên hóa đơn"),

        // 2. Giờ chuẩn — BR-01
        (SystemSettingKeys.StandardCheckInTime, "14:00", Groups.CheckInOut, "Giờ nhận phòng chuẩn - BR-01"),
        (SystemSettingKeys.StandardCheckOutTime, "12:00", Groups.CheckInOut, "Giờ trả phòng chuẩn - BR-01"),
        (SystemSettingKeys.OvernightStartHour, "22", Groups.CheckInOut, "Giờ mở gói qua đêm - BR-13"),
        (SystemSettingKeys.OvernightEndHour, "10", Groups.CheckInOut, "Giờ kết thúc gói qua đêm sáng hôm sau - BR-13"),

        // 3. Thuế & làm tròn — BR-04
        (SystemSettingKeys.VatRate, "0.08", Groups.Tax, "Thuế suất VAT - BR-04"),
        (SystemSettingKeys.RoundingUnit, "1000", Groups.Tax, "Đơn vị làm tròn tổng tiền - BR-04"),

        // 4. Phụ thu — BR-03
        (SystemSettingKeys.EarlyCheckInBoundaryHour, "9", Groups.Surcharge, "Mốc giờ chia hai bậc nhận phòng sớm"),
        (SystemSettingKeys.EarlyCheckInBefore09Rate, "0.5", Groups.Surcharge, "Nhận phòng trước mốc sớm - 50% giá đêm"),
        (SystemSettingKeys.EarlyCheckIn09To14Rate, "0.3", Groups.Surcharge, "Nhận phòng từ mốc sớm tới giờ chuẩn - 30% giá đêm"),
        (SystemSettingKeys.LateCheckOutTier1EndHour, "15", Groups.Surcharge, "Mốc giờ kết thúc bậc trả trễ thứ nhất"),
        (SystemSettingKeys.LateCheckOut12To15Rate, "0.3", Groups.Surcharge, "Trả phòng bậc 1 - 30% giá đêm"),
        (SystemSettingKeys.LateCheckOutFullNightHour, "18", Groups.Surcharge, "Trả sau mốc này thì tính thêm 1 đêm"),
        (SystemSettingKeys.LateCheckOut15To18Rate, "0.5", Groups.Surcharge, "Trả phòng bậc 2 - 50% giá đêm"),
        (SystemSettingKeys.HourlyGraceMinutes, "20", Groups.Surcharge, "Phút lẻ được bỏ qua khi tính giờ, lẻ quá mức này mới lên 1 giờ - BR-13"),

        // 5. Cọc & hủy — BR-05
        (SystemSettingKeys.DepositNights, "1", Groups.Cancellation, "Mức cọc đề xuất, tính theo số đêm"),
        (SystemSettingKeys.HoldUntilHour, "18", Groups.Cancellation, "Giờ hết hạn giữ chỗ đơn chưa cọc - BR-05"),
        (SystemSettingKeys.CancelFeeOver48hRate, "0", Groups.Cancellation, "Hủy trước 48 giờ - hoàn 100% cọc"),
        (SystemSettingKeys.CancelFee24To48hRate, "0.5", Groups.Cancellation, "Hủy 24-48 giờ - thu 50% cọc"),
        (SystemSettingKeys.CancelFeeUnder24hRate, "1", Groups.Cancellation, "Hủy dưới 24 giờ hoặc no-show - thu 100% cọc"),

        // 6. Hạn mức nghiệp vụ
        (SystemSettingKeys.ReceptionistMaxDiscountAmount, "200000", Groups.Limit, "Hạn mức giảm giá của lễ tân theo số tiền"),
        (SystemSettingKeys.ReceptionistMaxDiscountRate, "0.1", Groups.Limit, "Hạn mức giảm giá của lễ tân theo tỷ lệ"),
        (SystemSettingKeys.AllowSellWhenOutOfStock, "false", Groups.Limit, "Cho phép bán dịch vụ khi hết tồn - BR-12"),
        (SystemSettingKeys.LoyalGuestStayThreshold, "3", Groups.Limit, "Số lần lưu trú để được gắn nhãn khách quen"),
        (SystemSettingKeys.ChildAgeLimit, "12", Groups.Limit, "Tuổi tối đa tính là trẻ em, không thu phụ thu thêm người"),
        (SystemSettingKeys.CashDifferenceThreshold, "50000", Groups.Limit, "Ngưỡng chênh lệch tiền mặt khi đóng ca - vượt thì cảnh báo - SCR-F08"),

        // 7. Thanh toán VietQR & Email tự động
        (SystemSettingKeys.BankBin, "970419", Groups.Payment, "Mã BIN ngân hàng VietQR (VD: 970419 = Ngân hàng NCB, 970422 = MB Bank)"),
        (SystemSettingKeys.BankName, "Ngân hàng NCB", Groups.Payment, "Tên hiển thị ngân hàng thụ hưởng"),
        (SystemSettingKeys.BankAccountNo, "9704198526191432198", Groups.Payment, "Số thẻ / Số tài khoản nhận thanh toán"),
        (SystemSettingKeys.BankAccountName, "NGUYEN VAN A", Groups.Payment, "Tên chủ thẻ / chủ tài khoản ngân hàng"),
        (SystemSettingKeys.EmailEnabled, "true", Groups.Payment, "Tự động gửi email xác nhận đặt phòng và hóa đơn"),
        (SystemSettingKeys.EmailSenderName, "AURA Boutique Hotel & Resort", Groups.Payment, "Tên người gửi email"),
        (SystemSettingKeys.EmailSenderAddress, "booking@aurahotel.vn", Groups.Payment, "Địa chỉ email gửi đi"),
        (SystemSettingKeys.EmailSmtpHost, "", Groups.Payment, "Máy chủ SMTP (để trống sẽ lưu bản xem trước tại máy chủ)"),
        (SystemSettingKeys.EmailSmtpPort, "587", Groups.Payment, "Cổng SMTP"),
        (SystemSettingKeys.EmailSmtpUser, "", Groups.Payment, "Tài khoản SMTP"),
        (SystemSettingKeys.EmailSmtpPass, "", Groups.Payment, "Mật khẩu ứng dụng SMTP"),

        // 8. Bảng giá linh hoạt theo Mùa / Cuối tuần / Ngày lễ
        (SystemSettingKeys.EnableDynamicPricing, "true", Groups.DynamicPricing, "Bật tính giá tự động theo cuối tuần và mùa lễ"),
        (SystemSettingKeys.WeekendSurchargeRate, "0.15", Groups.DynamicPricing, "Tỷ lệ tăng giá đêm cuối tuần (0.15 = +15%)"),
        (SystemSettingKeys.WeekendDays, "Friday,Saturday", Groups.DynamicPricing, "Các đêm trong tuần áp dụng giá cuối tuần"),
        (SystemSettingKeys.SeasonalRulesJson, DefaultSeasonalRulesJson, Groups.DynamicPricing, "Danh sách quy tắc giá theo mùa / lễ Tết (JSON)")
    };

    /// <summary>Nhãn tiếng Việt của nhóm, dùng cho thông báo và nhật ký.</summary>
    public static string GroupDisplayName(string group) => group switch
    {
        Groups.Hotel => "Thông tin khách sạn",
        Groups.CheckInOut => "Giờ chuẩn",
        Groups.Tax => "Thuế & làm tròn",
        Groups.Surcharge => "Phụ thu",
        Groups.Cancellation => "Chính sách cọc & hủy",
        Groups.Limit => "Hạn mức nghiệp vụ",
        Groups.Payment => "Thanh toán VietQR & Email",
        Groups.DynamicPricing => "Bảng giá linh hoạt & Mùa vụ",
        _ => group
    };
}
