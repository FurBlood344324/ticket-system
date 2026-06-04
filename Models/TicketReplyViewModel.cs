using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class TicketReplyViewModel
{
    [Required(ErrorMessage = "Cevap metni zorunludur.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Cevap 2-1000 karakter arasında olmalıdır.")]
    public string Message { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Harcanan sure 0 veya daha buyuk olmalidir.")]
    public int? TimeSpentMinutes { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;
}
