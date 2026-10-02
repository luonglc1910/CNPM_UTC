namespace HotelManagement.Web.Security;

/// <summary>
/// Tên các Authentication Scheme dùng xuyên suốt hệ thống.
/// Khai báo tập trung ở đây để tránh hard-code chuỗi rải rác.
/// </summary>
public static class AppSchemes
{
    /// <summary>Cookie xác thực cho Nhân viên nội bộ (Staff/Admin).</summary>
    public const string Staff = "StaffCookie";

    /// <summary>Cookie xác thực cho Khách hàng vãng lai trên Portal.</summary>
    public const string Client = "ClientCookie";
}
