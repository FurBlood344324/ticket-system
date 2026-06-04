using Microsoft.AspNetCore.SignalR;
using TicketSupport.Hubs;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class RealTimeNotificationService
{
    private readonly IHubContext<TicketHub> ticketHubContext;
    private readonly IHubContext<NotificationHub> notificationHubContext;

    public RealTimeNotificationService(
        IHubContext<TicketHub> ticketHubContext,
        IHubContext<NotificationHub> notificationHubContext)
    {
        this.ticketHubContext = ticketHubContext;
        this.notificationHubContext = notificationHubContext;
    }

    public Task BroadcastTicketUpdatedAsync(SupportTicket ticket, string summary)
    {
        return ticketHubContext.Clients.Group(TicketHub.GetGroupName(ticket.Id))
            .SendAsync("TicketUpdated", CreateTicketPayload(ticket, summary));
    }

    public Task BroadcastTicketAssignedAsync(SupportTicket ticket)
    {
        return ticketHubContext.Clients.Group(TicketHub.GetGroupName(ticket.Id))
            .SendAsync("TicketAssigned", CreateTicketPayload(ticket, "Talep atandi."));
    }

    public Task BroadcastNewReplyAsync(SupportTicket ticket, TicketReply reply)
    {
        return ticketHubContext.Clients.Group(TicketHub.GetGroupName(ticket.Id)).SendAsync("NewReply", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            status = ticket.Status.ToString(),
            assignedSupportName = ticket.AssignedSupportName,
            reply = new
            {
                id = reply.Id,
                authorId = reply.AuthorId,
                authorName = reply.AuthorName,
                authorRole = reply.AuthorRole.ToString(),
                isInternal = reply.IsInternal,
                message = reply.Message,
                createdAt = reply.CreatedAt.ToString("O")
            },
            summary = $"{reply.AuthorName} yeni bir yanit ekledi."
        });
    }

    public Task BroadcastStatusChangedAsync(SupportTicket ticket, TicketStatus oldStatus, TicketStatus newStatus)
    {
        return ticketHubContext.Clients.Group(TicketHub.GetGroupName(ticket.Id)).SendAsync("StatusChanged", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            status = newStatus.ToString(),
            oldStatus = oldStatus.ToString(),
            newStatus = newStatus.ToString(),
            assignedSupportName = ticket.AssignedSupportName,
            createdAt = DateTime.UtcNow.ToString("O")
        });
    }

    public Task SendSlaWarningAsync(SupportTicket ticket, IReadOnlyCollection<int> userIds, string summary)
    {
        if (userIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return notificationHubContext.Clients.Users(userIds.Select(id => id.ToString())).SendAsync("SlaWarning", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            summary,
            status = ticket.Status.ToString(),
            dueDate = ticket.DueDate?.ToString("O"),
            url = $"/Tickets/Details/{ticket.Id}"
        });
    }

    public Task SendMentionNotificationAsync(SupportTicket ticket, IReadOnlyCollection<int> userIds, string message)
    {
        if (userIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        return notificationHubContext.Clients.Users(userIds.Select(id => id.ToString())).SendAsync("MentionNotification", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            message,
            url = $"/Tickets/Details/{ticket.Id}"
        });
    }

    public async Task SendUserNotificationAsync(UserNotification notification, int unreadCount)
    {
        var userKey = notification.UserId.ToString();

        await notificationHubContext.Clients.User(userKey).SendAsync("NotificationReceived", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            url = notification.Url,
            notificationType = notification.NotificationType.ToString(),
            createdAt = notification.CreatedAt.ToString("O")
        });

        await notificationHubContext.Clients.User(userKey).SendAsync("NotificationCountUpdated", unreadCount);
    }

    private static object CreateTicketPayload(SupportTicket ticket, string summary)
    {
        return new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            status = ticket.Status.ToString(),
            assignedSupportName = ticket.AssignedSupportName,
            lastUpdatedAt = ticket.LastUpdatedAt.ToString("O"),
            summary
        };
    }
}
