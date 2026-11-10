using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Web.Models.Entities;

/// <summary>Hồ sơ khách lưu trú — FR-B01. Không bao giờ xóa cứng.</summary>
public class Guest : BaseEntity
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public GuestIdType IdType { get; set; } = GuestIdType.CitizenId;

    /// <summary>Số CCCD/CMND/Hộ chiếu — duy nhất toàn hệ thống.</summary>
    [MaxLength(20)]
    public string IdNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    [MaxLength(50)]
    public string Nationality { get; set; } = "Việt Nam";

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    // ── Trường bổ sung cho luồng đăng nhập khách hàng (ClientCookie) ──

    /// <summary>Chuỗi băm PBKDF2 — null khi chưa có tài khoản đăng nhập (FR-B01).</summary>
    [MaxLength(200)]
    public string? PasswordHash { get; set; }

    /// <summary>false khi Admin khóa tài khoản khách hàng.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Địa chỉ thường trú — cần cho khai báo tạm trú (FR-B05).</summary>
    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // ── Loyalty / Rewards (Phase 4) ──
    public MemberTier Tier { get; set; } = MemberTier.Standard;

    /// <summary>Điểm thưởng tích lũy có thể dùng để đổi quà hoặc giảm giá.</summary>
    public int RewardPoints { get; set; }

    /// <summary>Danh sách hạn chế — FR-B06. Chỉ cảnh báo, không tự chặn giao dịch.</summary>
    public bool IsBlacklisted { get; set; }

    [MaxLength(500)]
    public string? BlacklistReason { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<StayGuest> StayGuests { get; set; } = new List<StayGuest>();
}
