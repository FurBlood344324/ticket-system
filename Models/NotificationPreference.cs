using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [MaxLength(60)]
    public string EventType { get; set; } = string.Empty;

    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;

    public AppUser? User { get; set; }
}
