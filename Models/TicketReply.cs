namespace TicketSupport.Models;

public class TicketReply
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public UserRole AuthorRole { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
    public int? TemplateId { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public bool IsAiSuggested { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CannedResponse? Template { get; set; }
    public SupportTicket? Ticket { get; set; }
    public List<TicketAttachment> Attachments { get; set; } = [];
}
