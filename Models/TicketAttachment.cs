namespace TicketSupport.Models;

public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? ReplyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupportTicket? Ticket { get; set; }
    public TicketReply? Reply { get; set; }
}
