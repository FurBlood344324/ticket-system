using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketSupport.Hubs;

[Authorize]
public class TicketHub : Hub
{
    public Task JoinTicketGroup(int ticketId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(ticketId));
    }

    public Task LeaveTicketGroup(int ticketId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(ticketId));
    }

    public static string GetGroupName(int ticketId)
    {
        return $"ticket-{ticketId}";
    }
}
