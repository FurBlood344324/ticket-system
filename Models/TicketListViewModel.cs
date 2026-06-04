namespace TicketSupport.Models;

public class TicketListViewModel
{
    public TicketStatus? StatusFilter { get; set; }
    public TicketPriority? PriorityFilter { get; set; }
    public TicketCategory? CategoryFilter { get; set; }
    public int? TagFilter { get; set; }
    public List<SupportTicket> Tickets { get; set; } = [];
}
