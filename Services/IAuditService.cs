namespace HotelManagement.Web.Services;

/// <summary>
/// Ghi nhật ký thao tác — BR-11, FR-G07.
/// Bản ghi chỉ được thêm vào ChangeTracker; lời gọi SaveChangesAsync do nơi gọi thực hiện,
/// để dòng log nằm chung transaction với thay đổi nghiệp vụ (00-conventions.md mục 7).
/// </summary>
public interface IAuditService
{
    void Log(
        string action,
        string entityType,
        string? entityId = null,
        string? reason = null,
        string? oldValue = null,
        string? newValue = null);

    /// <summary>Ghi log và lưu ngay — dùng khi thao tác không kèm thay đổi nghiệp vụ nào khác.</summary>
    Task LogAndSaveAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? reason = null);

    /// <summary>
    /// Ghi log cho một tài khoản cụ thể thay vì người đang đăng nhập — dùng cho sự kiện
    /// đăng nhập thất bại, lúc đó chưa có ClaimsPrincipal nào.
    /// </summary>
    void LogForUser(
        string action,
        string userName,
        Models.Entities.Employee? employee = null,
        string? reason = null);
}

/// <summary>Tên hành động chuẩn, tránh gõ chuỗi tự do rải rác trong code.</summary>
public static class AuditActions
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string PasswordChanged = "PasswordChanged";
    public const string AccessDenied = "AccessDenied";
}
