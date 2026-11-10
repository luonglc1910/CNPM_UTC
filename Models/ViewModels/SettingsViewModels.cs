using System.ComponentModel.DataAnnotations;
using HotelManagement.Web.Services;

namespace HotelManagement.Web.Models.ViewModels;

/// <summary>
/// Toàn bộ màn hình cấu hình — SCR-A12. Sáu nhóm, mỗi nhóm là một form riêng nên lưu nhóm này
/// không đụng nhóm kia, và "Khôi phục mặc định" cũng theo từng nhóm.
/// </summary>
public class SettingsViewModel
{
    public HotelInfoSettings Hotel { get; set; } = new();
    public CheckInOutSettings CheckInOut { get; set; } = new();
    public TaxSettings Tax { get; set; } = new();
    public SurchargeSettings Surcharge { get; set; } = new();
    public CancellationSettings Cancellation { get; set; } = new();
    public LimitSettings Limit { get; set; } = new();
    public PaymentAndEmailSettings Payment { get; set; } = new();
    public DynamicPricingSettings DynamicPricing { get; set; } = new();

    /// <summary>Nhóm vừa được lưu hoặc vừa lỗi — để cuộn thẳng tới thẻ đó.</summary>
    public string? ActiveGroup { get; set; }
}

/// <summary>Nhóm 1 — thông tin khách sạn, in trên hóa đơn.</summary>
public class HotelInfoSettings
{
    [Display(Name = "Tên khách sạn")]
    [Required(ErrorMessage = "Vui lòng nhập tên khách sạn.")]
    [MaxLength(200, ErrorMessage = "Tên khách sạn tối đa 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ")]
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [MaxLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [MaxLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Mã số thuế")]
    [RegularExpression(@"^\d{10}(-\d{3})?$",
        ErrorMessage = "Mã số thuế gồm 10 chữ số, đơn vị phụ thuộc thêm hậu tố dạng -001.")]
    public string? TaxCode { get; set; }

    /// <summary>Đường dẫn logo hiện tại (chỉ đọc); upload file mới qua ô riêng trên form.</summary>
    public string? LogoPath { get; set; }
}

/// <summary>Nhóm 2 — giờ chuẩn, BR-01.</summary>
public class CheckInOutSettings
{
    [Display(Name = "Giờ nhận phòng chuẩn")]
    [Required(ErrorMessage = "Vui lòng nhập giờ nhận phòng chuẩn.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ phải theo dạng HH:mm, ví dụ 14:00.")]
    public string StandardCheckInTime { get; set; } = "14:00";

    [Display(Name = "Giờ trả phòng chuẩn")]
    [Required(ErrorMessage = "Vui lòng nhập giờ trả phòng chuẩn.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ phải theo dạng HH:mm, ví dụ 12:00.")]
    public string StandardCheckOutTime { get; set; } = "12:00";

    // Gói qua đêm — BR-13. Chỉ nhập giờ nguyên: gói mở 22:00 đóng 10:00 là cách khách sạn rao
    // với khách, thêm phần phút vào chỉ làm câu quảng cáo khó đọc mà không đổi được gì trong tính tiền.
    [Display(Name = "Giờ mở gói qua đêm")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int OvernightStartHour { get; set; } = 22;

    [Display(Name = "Giờ kết thúc gói qua đêm (sáng hôm sau)")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int OvernightEndHour { get; set; } = 10;
}

/// <summary>Nhóm 3 — thuế và làm tròn, BR-04.</summary>
public class TaxSettings
{
    [Display(Name = "VAT (%)")]
    [Range(0, 100, ErrorMessage = "VAT nằm trong khoảng 0–100%.")]
    public decimal VatPercent { get; set; } = 8;

    [Display(Name = "Đơn vị làm tròn (₫)")]
    [Range(1, 1_000_000, ErrorMessage = "Đơn vị làm tròn phải lớn hơn 0.")]
    public int RoundingUnit { get; set; } = 1000;
}

/// <summary>Nhóm 4 — phụ thu, BR-03. Bảy dòng của bảng BR-03, mỗi dòng một mốc giờ và một tỷ lệ.</summary>
public class SurchargeSettings
{
    [Display(Name = "Mốc giờ nhận phòng sớm bậc 1")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int EarlyCheckInBoundaryHour { get; set; } = 9;

    [Display(Name = "Nhận phòng trước mốc trên (% giá đêm)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal EarlyCheckInBeforeBoundaryPercent { get; set; } = 50;

    [Display(Name = "Nhận phòng từ mốc trên tới giờ chuẩn (% giá đêm)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal EarlyCheckInToStandardPercent { get; set; } = 30;

    [Display(Name = "Mốc giờ kết thúc trả trễ bậc 1")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int LateCheckOutTier1EndHour { get; set; } = 15;

    [Display(Name = "Trả phòng bậc 1 (% giá đêm)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal LateCheckOutTier1Percent { get; set; } = 30;

    [Display(Name = "Mốc giờ tính thêm 1 đêm")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int LateCheckOutFullNightHour { get; set; } = 18;

    [Display(Name = "Trả phòng bậc 2 (% giá đêm)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal LateCheckOutTier2Percent { get; set; } = 50;

    [Display(Name = "Phút lẻ được bỏ qua khi tính giờ")]
    [Range(0, 59, ErrorMessage = "Số phút bỏ qua nằm trong khoảng 0–59.")]
    public int HourlyGraceMinutes { get; set; } = 20;
}

/// <summary>Nhóm 5 — chính sách cọc và hủy, BR-05.</summary>
public class CancellationSettings
{
    [Display(Name = "Mức cọc đề xuất (số đêm)")]
    [Range(0, 30, ErrorMessage = "Mức cọc đề xuất từ 0 đến 30 đêm.")]
    public int DepositNights { get; set; } = 1;

    [Display(Name = "Giờ hết hạn giữ chỗ đơn chưa cọc")]
    [Range(0, 23, ErrorMessage = "Mốc giờ nằm trong khoảng 0–23.")]
    public int HoldUntilHour { get; set; } = 18;

    [Display(Name = "Phí hủy khi hủy ≥ 48 giờ (% cọc)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal Over48hPercent { get; set; }

    [Display(Name = "Phí hủy khi hủy 24–48 giờ (% cọc)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal Between24And48hPercent { get; set; } = 50;

    [Display(Name = "Phí hủy khi hủy < 24 giờ / no-show (% cọc)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal Under24hPercent { get; set; } = 100;
}

/// <summary>Nhóm 6 — hạn mức nghiệp vụ.</summary>
public class LimitSettings
{
    [Display(Name = "Hạn mức giảm giá của lễ tân (₫)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Hạn mức không được âm.")]
    public decimal ReceptionistMaxDiscountAmount { get; set; } = 200_000;

    [Display(Name = "Hạn mức giảm giá của lễ tân (% hóa đơn)")]
    [Range(0, 100, ErrorMessage = "Tỷ lệ nằm trong khoảng 0–100%.")]
    public decimal ReceptionistMaxDiscountPercent { get; set; } = 10;

    [Display(Name = "Cho phép bán dịch vụ khi hết tồn kho")]
    public bool AllowSellWhenOutOfStock { get; set; }

    [Display(Name = "Số lần lưu trú để tính khách quen")]
    [Range(1, 100, ErrorMessage = "Số lần lưu trú từ 1 đến 100.")]
    public int LoyalGuestStayThreshold { get; set; } = 3;

    [Display(Name = "Tuổi tối đa tính là trẻ em")]
    [Range(0, 18, ErrorMessage = "Tuổi trẻ em từ 0 đến 18.")]
    public int ChildAgeLimit { get; set; } = 12;
}

/// <summary>Nhóm 7 — Thanh toán VietQR &amp; Email tự động (Chức năng 1 &amp; 2).</summary>
public class PaymentAndEmailSettings
{
    [Display(Name = "Mã BIN Ngân hàng (VietQR)")]
    [Required(ErrorMessage = "Vui lòng nhập mã BIN ngân hàng.")]
    public string BankBin { get; set; } = "970422";

    [Display(Name = "Tên Ngân hàng")]
    [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng.")]
    public string BankName { get; set; } = "MB Bank (Ngân hàng Quân Đội)";

    [Display(Name = "Số tài khoản nhận tiền")]
    [Required(ErrorMessage = "Vui lòng nhập số tài khoản.")]
    public string BankAccountNo { get; set; } = "088899998888";

    [Display(Name = "Chủ tài khoản")]
    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản.")]
    public string BankAccountName { get; set; } = "KHACH SAN AURA RESORT";

    [Display(Name = "Tự động gửi Email xác nhận đặt phòng & hóa đơn")]
    public bool EmailEnabled { get; set; } = true;

    [Display(Name = "Tên người gửi Email")]
    public string EmailSenderName { get; set; } = "AURA Boutique Hotel & Resort";

    [Display(Name = "Địa chỉ Email gửi")]
    public string EmailSenderAddress { get; set; } = "booking@aurahotel.vn";

    [Display(Name = "Máy chủ SMTP (tùy chọn)")]
    public string? EmailSmtpHost { get; set; }

    [Display(Name = "Cổng SMTP")]
    public int EmailSmtpPort { get; set; } = 587;

    [Display(Name = "Tài khoản SMTP")]
    public string? EmailSmtpUser { get; set; }

    [Display(Name = "Mật khẩu ứng dụng SMTP")]
    [DataType(DataType.Password)]
    public string? EmailSmtpPass { get; set; }

    public bool EmailSmtpPasswordConfigured { get; set; }
}

/// <summary>Nhóm 8 — Bảng giá linh hoạt theo Mùa / Cuối tuần / Ngày lễ (Chức năng 5).</summary>
public class DynamicPricingSettings
{
    [Display(Name = "Bật tính giá linh hoạt (Cuối tuần & Mùa vụ)")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Phụ thu đêm cuối tuần (%)")]
    [Range(-50, 200, ErrorMessage = "Tỷ lệ cuối tuần từ -50% đến +200%.")]
    public decimal WeekendSurchargePercent { get; set; } = 15;

    [Display(Name = "Các đêm tính giá cuối tuần")]
    public string WeekendDays { get; set; } = "Friday,Saturday";

    public List<SeasonalPricingRule> Rules { get; set; } = new();
}

public class AddSeasonalRuleInput
{
    [Required(ErrorMessage = "Vui lòng nhập tên đợt lễ / mùa vụ.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(3);

    [Range(-80, 300, ErrorMessage = "Mức điều chỉnh giá từ -80% đến +300%.")]
    public decimal PercentChange { get; set; } = 20;

    public string? RoomTypeIds { get; set; }
}
