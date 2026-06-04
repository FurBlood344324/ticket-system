namespace TicketSupport.Models;

public class SupportTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int EscalationLevel { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? AssignedSupportId { get; set; }
    public string? AssignedSupportName { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public List<TicketTagRelation> Tags { get; set; } = [];
    public List<TicketReply> Replies { get; set; } = [];
    public List<CustomerSatisfaction> SatisfactionEntries { get; set; } = [];
    public List<TicketAttachment> Attachments { get; set; } = [];
    public bool IsOverdue => DueDate.HasValue
        && Status is not TicketStatus.Solved
        && Status is not TicketStatus.Closed
        && Status is not TicketStatus.Cancelled
        && DueDate.Value < DateTime.UtcNow;
}
