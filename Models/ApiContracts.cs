using System.ComponentModel.DataAnnotations;

namespace TicketSupport.Models;

public class TicketListQueryRequest
{
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
    public int? DepartmentId { get; set; }
    public List<int> TagIds { get; set; } = [];
    public int? AssignedToId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsUnassigned { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateTicketRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1200, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public int? SelectedTemplateId { get; set; }
}

public class UpdateTicketRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1200, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required]
    public TicketCategory Category { get; set; } = TicketCategory.Other;
}

public class AddTicketReplyRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string Message { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    [Range(0, int.MaxValue)]
    public int? TimeSpentMinutes { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;
}

public class UpdateTicketStatusRequest
{
    [Required]
    public TicketStatus Status { get; set; }
}

public class UpdateTicketPriorityRequest
{
    [Required]
    public TicketPriority Priority { get; set; }
}

public class UpdateProfileRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfileImageUrl { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(10)]
    public string PreferredLanguage { get; set; } = "tr";

    [StringLength(500)]
    public string? Signature { get; set; }

    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool DarkMode { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateAdminUserRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }
}

public class TicketSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketCategory Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? AssignedSupportId { get; set; }
    public string? AssignedSupportName { get; set; }
    public DepartmentDto? Department { get; set; }
    public IReadOnlyCollection<TagDto> Tags { get; set; } = [];
}

public class TicketDetailDto : TicketSummaryDto
{
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int EscalationLevel { get; set; }
    public IReadOnlyCollection<TicketReplyDto> Replies { get; set; } = [];
    public IReadOnlyCollection<TicketAttachmentDto> Attachments { get; set; } = [];
}

public class TicketReplyDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public UserRole AuthorRole { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<TicketAttachmentDto> Attachments { get; set; } = [];
}

public class TicketAttachmentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? ReplyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TicketAuditDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
}

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public bool DarkMode { get; set; }
    public string? Signature { get; set; }
    public bool IsActive { get; set; }
}

public class KnowledgeArticleSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public string? CategoryName { get; set; }
}

public class KnowledgeArticleDetailDto : KnowledgeArticleSummaryDto
{
    public string Content { get; set; } = string.Empty;
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int UnassignedTickets { get; set; }
    public int PublishedKnowledgeArticles { get; set; }
}
