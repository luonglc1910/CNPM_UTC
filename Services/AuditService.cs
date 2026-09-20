using HotelManagement.Web.Data;
using HotelManagement.Web.Models;
using HotelManagement.Web.Models.Entities;
using HotelManagement.Web.Security;

namespace HotelManagement.Web.Services;

/// <inheritdoc />
public class AuditService : IAuditService
{
    private readonly HotelDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(HotelDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Log(
        string action,
        string entityType,
        string? entityId = null,
        string? reason = null,
        string? oldValue = null,
        string? newValue = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        EmployeeRole? role = null;
        if (Enum.TryParse<EmployeeRole>(user?.GetRole(), out var parsedRole))
        {
            role = parsedRole;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.Now,
            UserId = user?.GetEmployeeId(),
            UserName = user?.GetUserName() ?? "(khách)",
            UserRole = role,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    public async Task LogAndSaveAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? reason = null)
    {
        Log(action, entityType, entityId, reason);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void LogForUser(
        string action,
        string userName,
        Employee? employee = null,
        string? reason = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.Now,
            UserId = employee?.Id,
            UserName = userName,
            UserRole = employee?.Role,
            Action = action,
            EntityType = nameof(Employee),
            EntityId = employee?.Id.ToString(),
            Reason = reason,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
}
