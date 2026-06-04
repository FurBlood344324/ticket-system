using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class AdminUserCreateViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Ad soyad 3-80 karakter arasında olmalıdır.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta yazın.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    public UserRole Role { get; set; } = UserRole.Support;

    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    [StringLength(100)]
    [Display(Name = "Ünvan")]
    public string? JobTitle { get; set; }

    [StringLength(20)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class AdminUserEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Ad soyad 3-80 karakter arasında olmalıdır.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    public UserRole Role { get; set; }

    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    [StringLength(100)]
    [Display(Name = "Ünvan")]
    public string? JobTitle { get; set; }

    [StringLength(20)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }
}

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(80, MinimumLength = 2)]
    [Display(Name = "Departman Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class SlaPolicyFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Öncelik")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Range(1, 100000, ErrorMessage = "Yanıt süresi 1 dakikadan büyük olmalı.")]
    [Display(Name = "İlk Yanıt Süresi (dakika)")]
    public int ResponseTimeMinutes { get; set; } = 60;

    [Range(1, 1000000, ErrorMessage = "Çözüm süresi 1 dakikadan büyük olmalı.")]
    [Display(Name = "Çözüm Süresi (dakika)")]
    public int ResolutionTimeMinutes { get; set; } = 480;

    [Display(Name = "Yalnızca Mesai Saatleri")]
    public bool BusinessHoursOnly { get; set; }
}

public class TicketTemplateFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Kategori")]
    public TicketCategory Category { get; set; } = TicketCategory.Other;

    [Display(Name = "Öncelik")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required(ErrorMessage = "İçerik zorunludur.")]
    [StringLength(5000, MinimumLength = 3)]
    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class CannedResponseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori zorunludur.")]
    [StringLength(80)]
    [Display(Name = "Kategori")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik zorunludur.")]
    [StringLength(4000, MinimumLength = 2)]
    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Ekip ile paylaşımlı")]
    public bool IsShared { get; set; } = true;
}

public class TagFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Etiket adı zorunludur.")]
    [StringLength(60, MinimumLength = 1)]
    [Display(Name = "Etiket Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Renk zorunludur.")]
    [StringLength(20)]
    [Display(Name = "Renk")]
    public string Color { get; set; } = "#3ad6e6";
}

public class RecurringTicketFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(1200, MinimumLength = 3)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Kategori")]
    public TicketCategory Category { get; set; } = TicketCategory.Other;

    [Display(Name = "Öncelik")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Display(Name = "Departman")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Cron ifadesi zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Cron İfadesi")]
    public string CronExpression { get; set; } = "0 9 * * 1";

    [Display(Name = "Sonraki Çalışma (yerel saat)")]
    [DataType(DataType.DateTime)]
    public DateTime? NextRunAtLocal { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class KnowledgeCategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Sıralama")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class KnowledgeArticleFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200, MinimumLength = 3)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Slug (boş bırakılırsa otomatik üretilir)")]
    public string? Slug { get; set; }

    [Display(Name = "Kategori")]
    public int? KnowledgeCategoryId { get; set; }

    [Required(ErrorMessage = "İçerik zorunludur.")]
    [StringLength(5000, MinimumLength = 3)]
    [Display(Name = "İçerik (Markdown)")]
    public string Content { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Meta Anahtar Kelimeler")]
    public string? MetaKeywords { get; set; }

    [StringLength(300)]
    [Display(Name = "Meta Açıklama")]
    public string? MetaDescription { get; set; }

    [Display(Name = "Yayınla")]
    public bool IsPublished { get; set; }
}

/// <summary>
/// Yönetim panelinin genel bakış ekranında gösterilen özet sayılar.
/// </summary>
public class AdminOverviewViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int UnassignedTickets { get; set; }
    public int DepartmentCount { get; set; }
    public int SlaPolicyCount { get; set; }
    public int TemplateCount { get; set; }
    public int CannedResponseCount { get; set; }
    public int TagCount { get; set; }
    public int RecurringCount { get; set; }
    public int PublishedArticles { get; set; }
    public int CategoryCount { get; set; }
}
