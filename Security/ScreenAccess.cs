using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace HotelManagement.Web.Security;

/// <summary>
/// Trả lời câu hỏi "người đang đăng nhập có vào được màn hình này không?" bằng cách hỏi
/// đúng bộ máy phân quyền của ASP.NET Core, dựa trên chính attribute [Authorize] gắn trên
/// controller/action đó.
///
/// Mục đích: menu trái không còn chép tay điều kiện quyền. Sửa [Authorize] ở controller là
/// menu tự đổi theo — không bao giờ xảy ra cảnh nhìn thấy mục menu rồi bấm vào bị 403.
/// </summary>
public interface IScreenAccess
{
    Task<bool> CanAccessAsync(string controller, string action = "Index");
}

/// <inheritdoc />
public class ScreenAccess : IScreenAccess
{
    private readonly IActionDescriptorCollectionProvider _actions;
    private readonly IAuthorizationService _authorization;
    private readonly IAuthorizationPolicyProvider _policyProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Bộ nhớ đệm trong phạm vi một request — sidebar hỏi khoảng chục lần mỗi trang.</summary>
    private readonly Dictionary<string, bool> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ScreenAccess(
        IActionDescriptorCollectionProvider actions,
        IAuthorizationService authorization,
        IAuthorizationPolicyProvider policyProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _actions = actions;
        _authorization = authorization;
        _policyProvider = policyProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CanAccessAsync(string controller, string action = "Index")
    {
        var key = $"{controller}/{action}";

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var allowed = await EvaluateAsync(controller, action);
        _cache[key] = allowed;
        return allowed;
    }

    private async Task<bool> EvaluateAsync(string controller, string action)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return false;
        }

        var descriptor = _actions.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(d => string.Equals(d.ControllerName, controller, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(d.ActionName, action, StringComparison.OrdinalIgnoreCase))
            // Một tên action có thể ứng với cả GET và POST; menu luôn trỏ tới bản GET.
            .OrderByDescending(IsGettable)
            .FirstOrDefault();

        // Link trỏ tới action không tồn tại (đã xóa, gõ sai tên) thì ẩn luôn còn hơn để
        // người dùng bấm vào rồi nhận 404.
        if (descriptor is null)
        {
            return false;
        }

        if (descriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return true;
        }

        var authorizeData = descriptor.EndpointMetadata.OfType<IAuthorizeData>().ToArray();

        // Không có [Authorize] nào thì endpoint rơi vào FallbackPolicy khai báo ở Program.cs.
        var policy = authorizeData.Length > 0
            ? await AuthorizationPolicy.CombineAsync(_policyProvider, authorizeData)
            : await _policyProvider.GetFallbackPolicyAsync();

        if (policy is null)
        {
            return true;
        }

        var result = await _authorization.AuthorizeAsync(user, policy);
        return result.Succeeded;
    }

    private static bool IsGettable(ControllerActionDescriptor descriptor)
    {
        var httpMethods = descriptor.EndpointMetadata.OfType<HttpMethodMetadata>().ToArray();

        // Không khai báo verb nào = nhận mọi verb, kể cả GET.
        return httpMethods.Length == 0
               || httpMethods.Any(m => m.HttpMethods.Contains("GET", StringComparer.OrdinalIgnoreCase));
    }
}
