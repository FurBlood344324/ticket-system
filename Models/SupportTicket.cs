namespace TicketSupport.Models;

public class SupportTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? AssignedSupportId { get; set; }
    public string? AssignedSupportName { get; set; }
    public List<TicketReply> Replies { get; set; } = [];
}
