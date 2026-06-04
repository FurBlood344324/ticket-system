namespace TicketSupport.Models;

public class CustomerOrganization
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
}
