using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkflowEngine.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub untuk push notifikasi workflow secara real-time ke web client / Angular SPA.
/// </summary>
public class WorkflowNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userIdHeader = httpContext?.Request.Headers["X-User-Id"].FirstOrDefault()
                           ?? Context.UserIdentifier;

        if (!string.IsNullOrWhiteSpace(userIdHeader))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdHeader}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinUserGroup(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
    }

    public async Task LeaveUserGroup(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
    }
}
