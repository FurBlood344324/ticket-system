namespace TicketSupport.Models;

public class TicketDetailsViewModel
{
    public SupportTicket Ticket { get; set; } = new();
    public TicketReplyViewModel ReplyForm { get; set; } = new();
    public TicketEditViewModel EditForm { get; set; } = new();
    public TicketTimeEntryViewModel TimeEntryForm { get; set; } = new();
    public List<TicketTimelineItemViewModel> TimelineItems { get; set; } = [];
    public List<TicketAttachment> VisibleAttachments { get; set; } = [];
    public string DescriptionHtml { get; set; } = string.Empty;
    public int TotalTrackedMinutes { get; set; }
    public bool CanManageTicket { get; set; }
    public bool CanTrackTime { get; set; }
    public bool CanAssignToSelf { get; set; }
    public bool IsCustomerView { get; set; }
    public string SlaSummary { get; set; } = string.Empty;
    public string SlaClass { get; set; } = "text-bg-secondary";
    public string ResponseSlaSummary { get; set; } = string.Empty;
    public DateTime? ActiveTimerStartedAtUtc { get; set; }
}
