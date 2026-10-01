using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Sổ chi phí của một lượt lưu trú — FR-F01. Quan hệ 1-1 với Stay.
/// Khóa folio là điều kiện để chuyển sang thanh toán (BR-08).
/// </summary>
public class Folio : BaseEntity
{
    /// <summary>Mã folio dạng F-###### để in trên bảng kê tạm tính.</summary>
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public int StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    /// <summary>Đã khóa thì không thêm chi phí được nữa; chỉ Admin mở lại.</summary>
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public int? LockedBy { get; set; }

    public ICollection<FolioItem> Items { get; set; } = new List<FolioItem>();
    public Invoice? Invoice { get; set; }
}
