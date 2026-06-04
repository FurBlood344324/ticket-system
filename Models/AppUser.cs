using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
