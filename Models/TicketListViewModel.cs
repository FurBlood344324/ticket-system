namespace TicketSupport.Models;

public class TicketListViewModel
{
    public TicketStatus? StatusFilter { get; set; }
    public List<SupportTicket> Tickets { get; set; } = [];
}
