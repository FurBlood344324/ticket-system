namespace TicketSupport.Models;

public class TicketTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TicketTagRelation> TicketRelations { get; set; } = [];
}
