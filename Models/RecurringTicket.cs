namespace TicketSupport.Models;

public class RecurringTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastCreatedAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int CreatedById { get; set; }
}
