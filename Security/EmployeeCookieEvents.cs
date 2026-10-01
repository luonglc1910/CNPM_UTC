using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Web.Security;

/// <summary>
/// Sự kiện của cookie đăng nhập.
///
/// <see cref="ValidatePrincipal"/> là chỗ duy nhất làm được yêu cầu "người bị đổi vai trò bị
/// đăng xuất khỏi mọi phiên" (SCR-A11) và "khóa tài khoản có hiệu lực ngay" (SCR-A10):
/// cookie là bản chụp quyền tại thời điểm đăng nhập, sửa DB không làm nó tự đổi. Mỗi request
/// đối chiếu lại cookie với bản ghi nhân viên, lệch thì hủy cookie và bắt đăng nhập lại.
/// </summary>
public class EmployeeCookieEvents : CookieAuthenticationEvents
{
    private readonly HotelDbContext _db;

    public EmployeeCookieEvents(HotelDbContext db)
    {
        _db = db;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var employeeId = context.Principal?.GetEmployeeId();
        if (employeeId is null)
        {
            return;
        }

        var account = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Role, e.Status })
            .FirstOrDefaultAsync();

        // Tài khoản bị xóa, bị khóa, hoặc đã nghỉ việc: cookie hết giá trị ngay lập tức.
        if (account is null || account.Status != EmployeeStatus.Active)
        {
            await RejectAsync(context);
            return;
        }

        // Vai trò trong cookie khác vai trò thật: phải đăng nhập lại để nạp quyền mới.
        if (!string.Equals(context.Principal!.GetRole(), account.Role.ToString(), StringComparison.Ordinal))
        {
            await RejectAsync(context);
        }
    }

    /// <summary>
    /// Phân biệt "hết phiên" với "chưa từng đăng nhập": còn cookie mà vẫn bị từ chối
    /// nghĩa là phiên đã hết hạn (00-conventions.md mục 2).
    /// </summary>
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Cookies.ContainsKey("HotelAuth"))
        {
            context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "expired", "1");
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
