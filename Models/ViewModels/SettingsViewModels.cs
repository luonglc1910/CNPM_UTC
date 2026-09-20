using System.ComponentModel.DataAnnotations;

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
