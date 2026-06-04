namespace TicketSupport.Models;

public class TicketTimelineItemViewModel
{
    public DateTime OccurredAt { get; set; }
    public TicketReply? Reply { get; set; }
    public TicketAuditEvent? AuditEvent { get; set; }

    public bool IsReply => Reply is not null;
    public bool IsAudit => AuditEvent is not null;
}
