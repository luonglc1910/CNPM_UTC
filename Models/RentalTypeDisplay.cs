namespace HotelManagement.Web.Models;

/// <summary>
/// Cách gọi tên ba hình thức thuê trên giao diện và trong nhật ký — BR-13.
///
/// Gom vào một chỗ vì tên này xuất hiện ở rất nhiều nơi: danh sách đơn, chi tiết đơn,
/// sơ đồ phòng, màn trả phòng, hóa đơn và audit log. Để mỗi màn tự đặt tên thì chỉ cần
/// một chỗ gõ "theo giờ" còn chỗ khác gõ "tính giờ" là người dùng đã tưởng là hai thứ khác nhau.
/// </summary>
public static class RentalTypeDisplay
{
    public static string DisplayName(this RentalType type) => type switch
    {
        RentalType.Hourly => "Theo giờ",
        RentalType.Overnight => "Qua đêm",
        _ => "Theo ngày"
    };

    /// <summary>Lớp badge Bootstrap — mỗi hình thức một màu cố định để nhận ra ngay khi lướt bảng.</summary>
    public static string BadgeClass(this RentalType type) => type switch
    {
        RentalType.Hourly => "text-bg-warning",
        RentalType.Overnight => "text-bg-dark",
        _ => "text-bg-primary"
    };

    public static string IconName(this RentalType type) => type switch
    {
        RentalType.Hourly => "stopwatch",
        RentalType.Overnight => "moon-stars",
        _ => "calendar3"
    };

    /// <summary>Cách hiển thị khoảng thuê của một đơn/lượt ở đã lưu.</summary>
    public static string SpanText(this RentalType type, System.DateTime checkIn, System.DateTime checkOut, int nights, int hours)
        => type switch
        {
            // Đơn thuê giờ không có mốc giờ nào thật: đồng hồ chạy từ lúc check-in tới lúc trả
            // phòng. In một mốc giờ ra đây thì người đọc sẽ tưởng đó là giờ đã hẹn với khách.
            RentalType.Hourly => $"ngày {checkIn:dd/MM/yyyy} — tính giờ từ lúc check-in đến lúc trả phòng",
            RentalType.Overnight => $"{checkIn:HH\\:mm dd/MM} → {checkOut:HH\\:mm dd/MM}",
            _ => $"{checkIn:dd/MM/yyyy} → {checkOut:dd/MM/yyyy} ({nights} đêm)"
        };
}
