namespace HotelManagement.Web.Security;

/// <summary>Các claim tự định nghĩa của hệ thống.</summary>
public static class AppClaimTypes
{
    /// <summary>
    /// Có giá trị "true" khi tài khoản bị buộc đổi mật khẩu (FR-A08).
    /// Đặt trong cookie để MustChangePasswordFilter khỏi phải truy vấn DB mỗi request.
    /// </summary>
    public const string MustChangePassword = "must_change_password";
}
