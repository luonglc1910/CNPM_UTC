using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelManagement.Web.Security;

/// <summary>
/// Buộc người dùng có cờ MustChangePassword phải đổi mật khẩu trước khi dùng chức năng khác
/// — FR-A08, SCR-S02.
///
/// Cờ đọc từ claim nên không phát sinh truy vấn DB; sau khi đổi mật khẩu thành công,
/// AccountController phát hành lại cookie không còn claim này.
/// </summary>
public class MustChangePasswordFilter : IActionFilter
{
    /// <summary>Các action luôn cho qua để tránh chuyển hướng vòng lặp vô hạn.</summary>
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ChangePassword",
        "Logout",
        "Login"
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true || !user.MustChangePassword())
        {
            return;
        }

        // Trang công khai (đăng nhập, lỗi) không bị ép.
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var controller = context.RouteData.Values["controller"] as string;
        var action = context.RouteData.Values["action"] as string;

        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase)
            && AllowedActions.Contains(action ?? string.Empty))
        {
            return;
        }

        context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
