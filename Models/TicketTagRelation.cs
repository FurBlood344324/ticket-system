namespace TicketSupport.Models;

public class TicketTagRelation
{
    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }
    public int TagId { get; set; }
    public TicketTag? Tag { get; set; }
}
