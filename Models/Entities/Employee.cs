using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Nhân viên kiêm tài khoản đăng nhập — FR-A06, FR-A08.</summary>
public class Employee : BaseEntity
{
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? IdNumber { get; set; }

    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Chuỗi băm PBKDF2 — không bao giờ lưu mật khẩu gốc (NFR-02).</summary>
    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    public EmployeeRole Role { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Số lần đăng nhập sai liên tiếp; đủ 5 lần thì khóa tài khoản (FR-A08).</summary>
    public int FailedLoginCount { get; set; }

    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<CashierShift> Shifts { get; set; } = new List<CashierShift>();
}
