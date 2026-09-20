using System.Security.Claims;
using HotelManagement.Web.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelManagement.Web.Security;

/// <summary>
/// Gán Id nhân viên đang đăng nhập vào HotelDbContext.CurrentUserId để
/// ApplyAuditFields() điền được CreatedBy/UpdatedBy — NFR-07.
///
/// Dùng action filter thay vì middleware vì filter chạy trong đúng DI scope của request,
/// và không đụng tới đường chạy seed lúc khởi động (lúc đó không có HttpContext).
/// </summary>
public class CurrentUserFilter : IActionFilter
{
    private readonly HotelDbContext _db;

    public CurrentUserFilter(HotelDbContext db)
    {
        _db = db;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _db.CurrentUserId = context.HttpContext.User.GetEmployeeId();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

/// <summary>Đọc thông tin nhân viên từ ClaimsPrincipal.</summary>
public static class ClaimsPrincipalExtensions
{
    public static int? GetEmployeeId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name);

    /// <summary>Họ tên đầy đủ để hiển thị; thiếu thì lùi về tên đăng nhập.</summary>
    public static string GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.GivenName)
           ?? user.FindFirstValue(ClaimTypes.Name)
           ?? string.Empty;

    public static string? GetRole(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role);

    public static string GetRoleDisplayName(this ClaimsPrincipal user)
        => Roles.ToDisplayName(user.GetRole());

    public static bool MustChangePassword(this ClaimsPrincipal user)
        => user.FindFirstValue(AppClaimTypes.MustChangePassword) == "true";
}
