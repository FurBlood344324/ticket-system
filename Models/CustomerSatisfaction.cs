namespace TicketSupport.Models;

public class CustomerSatisfaction
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasinda olmalidir.")]
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsVisibleToCustomer { get; set; } = true;

    public SupportTicket? Ticket { get; set; }
}
