namespace TicketSupport.Models;

public class TicketTimeEntry
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public string? Description { get; set; }
    public TicketActivityType ActivityType { get; set; } = TicketActivityType.Work;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
