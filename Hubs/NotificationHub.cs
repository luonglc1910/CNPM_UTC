using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelManagement.Web.Hubs;

/// <summary>
/// SignalR Hub đẩy thông báo thời gian thực cho nhân viên quầy lễ tân &amp; quản lý (Chức năng 2).
/// </summary>
[AllowAnonymous]
public class NotificationHub : Hub
{
}

