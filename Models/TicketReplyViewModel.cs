using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class TicketReplyViewModel
{
    [Required(ErrorMessage = "Cevap metni zorunludur.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Cevap 2-1000 karakter arasında olmalıdır.")]
    public string Message { get; set; } = string.Empty;

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;
}
