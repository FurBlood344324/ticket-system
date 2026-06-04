using TicketSupport.Models;

namespace TicketSupport.Helpers;

/// <summary>
/// Enum değerleri için Türkçe görünen etiketler ve tema renk sınıfları.
/// </summary>
public static class DisplayLabels
{
    public static string Priority(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Düşük",
        TicketPriority.Medium => "Orta",
        TicketPriority.High => "Yüksek",
        TicketPriority.Critical => "Kritik",
        _ => priority.ToString()
    };

    public static string PriorityClass(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "cyan",
        TicketPriority.Medium => "green",
        TicketPriority.High => "warn",
        TicketPriority.Critical => "danger",
        _ => "off"
    };

    public static string Category(TicketCategory category) => category switch
    {
        TicketCategory.Hardware => "Donanım",
        TicketCategory.Software => "Yazılım",
        TicketCategory.Network => "Ağ",
        TicketCategory.Account => "Hesap",
        TicketCategory.Email => "E-posta",
        TicketCategory.Other => "Diğer",
        _ => category.ToString()
    };

    public static string Role(UserRole role) => role switch
    {
        UserRole.Customer => "Müşteri",
        UserRole.Support => "Destek",
        UserRole.Admin => "Yönetici",
        _ => role.ToString()
    };

    public static string RoleClass(UserRole role) => role switch
    {
        UserRole.Customer => "cyan",
        UserRole.Support => "green",
        UserRole.Admin => "purple",
        _ => "off"
    };

    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Açık",
        TicketStatus.InProgress => "İşlemde",
        TicketStatus.WaitingCustomer => "Müşteri Bekleniyor",
        TicketStatus.Solved => "Çözüldü",
        TicketStatus.Closed => "Kapalı",
        TicketStatus.Cancelled => "İptal",
        _ => status.ToString()
    };
}
