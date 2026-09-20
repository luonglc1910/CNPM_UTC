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

    // Những hành động dưới đây được ĐỌC NGƯỢC ở nơi khác, không chỉ để người đọc nhật ký xem:
    // SCR-G04 đếm hai hành động Override để ra cột "số lần bỏ qua cảnh báo". Bên ghi và bên
    // đếm lệch nhau một ký tự thì con số âm thầm về 0 mà không có lỗi nào báo ra.

    /// <summary>Vẫn tiếp tục dù khách nằm trong danh sách hạn chế — BR-11, SCR-B05.</summary>
    public const string OverrideBlacklistWarning = "OverrideBlacklistWarning";

    /// <summary>Vẫn bán dịch vụ khi tồn kho không đủ — BR-12.</summary>
    public const string OverrideNegativeStock = "OverrideNegativeStock";

    /// <summary>Mở danh sách khai báo tạm trú — dữ liệu cá nhân, SCR-B04.</summary>
    public const string ViewResidenceList = "ViewResidenceList";

    /// <summary>Đưa khách vào danh sách hạn chế — SCR-B05.</summary>
    public const string BlacklistAdd = "BlacklistAdd";

    /// <summary>Gỡ khách khỏi danh sách hạn chế — SCR-B05.</summary>
    public const string BlacklistRemove = "BlacklistRemove";
}
