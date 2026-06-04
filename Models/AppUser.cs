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
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Yeni alanlar
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    public int? OrganizationId { get; set; }
    public CustomerOrganization? Organization { get; set; }

    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool TwoFactorEnabled { get; set; } = false;

    [MaxLength(100)]
    public string? TwoFactorSecretKey { get; set; }

    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "tr";
    public bool DarkMode { get; set; } = false;

    [MaxLength(500)]
    public string? Signature { get; set; }

    public ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();
}
