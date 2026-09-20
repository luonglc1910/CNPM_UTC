using HotelManagement.Web.Models;

namespace HotelManagement.Web.Security;

/// <summary>
/// Hằng chuỗi vai trò dùng cho [Authorize(Roles = ...)] — attribute chỉ nhận hằng biên dịch
/// nên không truyền thẳng enum EmployeeRole vào được.
/// Giá trị phải khớp đúng tên enum vì claim ClaimTypes.Role lưu Role.ToString().
/// </summary>
public static class Roles
{
    public const string Admin = nameof(EmployeeRole.Admin);
    public const string Receptionist = nameof(EmployeeRole.Receptionist);

    /// <summary>
    /// Mọi vai trò đăng nhập được — hiện là Admin + Lễ tân.
    /// Dùng cho màn hình cả hai vai trò vào được; riêng thao tác chỉ dành cho Admin
    /// thì đánh thêm [Authorize(Roles = Roles.Admin)] ở cấp action.
    /// </summary>
    public const string All = Admin + "," + Receptionist;

    /// <summary>Nhãn tiếng Việt hiển thị trên giao diện.</summary>
    public static string ToDisplayName(string? role) => role switch
    {
        Admin => "Quản lý",
        Receptionist => "Lễ tân",
        _ => "Không xác định"
    };
}
