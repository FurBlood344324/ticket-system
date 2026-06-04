using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;

namespace TicketSupport.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ApplicationDbContext dbContext;

    public NotificationHub(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task SendNotification(string title, string message, string? url = null, string? notificationType = null)
    {
        return Clients.Caller.SendAsync("NotificationReceived", new
        {
            title,
            message,
            url,
            notificationType,
            createdAt = DateTime.UtcNow.ToString("O")
        });
    }

    public async Task MarkAsRead(int notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return;
        }

        var notification = await dbContext.UserNotifications
            .FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId.Value);

        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await dbContext.SaveChangesAsync();

        var unreadCount = await dbContext.UserNotifications
            .CountAsync(item => item.UserId == userId.Value && !item.IsRead);

        await Clients.Caller.SendAsync("NotificationMarkedAsRead", new
        {
            notificationId,
            unreadCount
        });
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return;
        }

        var unreadCount = await dbContext.UserNotifications
            .CountAsync(item => item.UserId == userId.Value && !item.IsRead);

        await Clients.Caller.SendAsync("NotificationCountUpdated", unreadCount);
    }

    private int? GetCurrentUserId()
    {
        var userIdText = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdText, out var userId) ? userId : null;
    }
}
