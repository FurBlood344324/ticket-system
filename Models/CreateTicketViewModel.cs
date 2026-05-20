using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "Başlık 5-120 karakter arasında olmalıdır.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(1200, MinimumLength = 10, ErrorMessage = "Açıklama 10-1200 karakter arasında olmalıdır.")]
    public string Description { get; set; } = string.Empty;
}
