using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class TicketTimeEntryViewModel
{
    [Range(1, 1440, ErrorMessage = "Dakika değeri 1 ile 1440 arasında olmalıdır.")]
    public int Minutes { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama 500 karakteri aşamaz.")]
    public string? Description { get; set; }

    [Required]
    public TicketActivityType ActivityType { get; set; } = TicketActivityType.Work;
}
