namespace TicketSupport.Models;

public class TicketAuditEvent
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
