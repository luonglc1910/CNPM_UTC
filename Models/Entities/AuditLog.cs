using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>
/// Nhật ký thao tác — FR-G07, BR-11, NFR-07.
/// Bảng chỉ ghi thêm: toàn bộ ứng dụng không có chức năng sửa hay xóa bản ghi ở đây.
/// Không kế thừa BaseEntity vì log không bao giờ bị cập nhật.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UserId { get; set; }

    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    public EmployeeRole? UserRole { get; set; }

    /// <summary>Tên hành động, ví dụ: VoidInvoice, ApplyDiscount, ChangeRoom.</summary>
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Tên bảng hoặc thực thể bị tác động.</summary>
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EntityId { get; set; }

    [MaxLength(2000)]
    public string? OldValue { get; set; }

    [MaxLength(2000)]
    public string? NewValue { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
