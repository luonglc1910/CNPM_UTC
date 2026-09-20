using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Bộ đếm cấp số chứng từ liên tục — FR-C04, FR-F05.
/// Mỗi cặp (tiền tố, kỳ) giữ một số cuối cùng đã cấp; tăng dần trong transaction có khóa dòng
/// để hai người thao tác cùng lúc không nhận trùng số. Không kế thừa BaseEntity vì đây là bảng
/// kỹ thuật nội bộ, không cần trường kiểm toán.
/// </summary>
public class NumberSequence
{
    public int Id { get; set; }

    /// <summary>Tiền tố loại chứng từ, ví dụ HD (hóa đơn), RSV (đơn đặt), F (folio).</summary>
    [MaxLength(20)]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Kỳ đặt lại số, ví dụ "202609" cho hóa đơn theo tháng hoặc "260920" cho đơn đặt theo ngày.
    /// Rỗng nghĩa là đánh số liên tục toàn cục, không đặt lại theo kỳ.
    /// </summary>
    [MaxLength(20)]
    public string Period { get; set; } = string.Empty;

    /// <summary>Số cuối cùng đã cấp cho cặp (tiền tố, kỳ).</summary>
    public long LastValue { get; set; }
}
