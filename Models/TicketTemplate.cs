namespace TicketSupport.Models;

public class TicketTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public string Content { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
