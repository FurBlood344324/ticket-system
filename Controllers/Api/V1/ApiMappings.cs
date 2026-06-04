using TicketSupport.Models;

namespace TicketSupport.Controllers.Api.V1;

internal static class ApiMappings
{
    public static TicketSummaryDto ToSummaryDto(this SupportTicket ticket)
    {
        return new TicketSummaryDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            Category = ticket.Category,
            CreatedAt = ticket.CreatedAt,
            LastUpdatedAt = ticket.LastUpdatedAt,
            DueDate = ticket.DueDate,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.CustomerName,
            AssignedSupportId = ticket.AssignedSupportId,
            AssignedSupportName = ticket.AssignedSupportName,
            Department = ticket.Department is null ? null : new DepartmentDto
            {
                Id = ticket.Department.Id,
                Name = ticket.Department.Name
            },
            Tags = ticket.Tags
                .Where(relation => relation.Tag is not null)
                .Select(relation => new TagDto
                {
                    Id = relation.Tag!.Id,
                    Name = relation.Tag.Name,
                    Color = relation.Tag.Color
                })
                .ToList()
        };
    }

    public static TicketDetailDto ToDetailDto(this SupportTicket ticket)
    {
        var detail = new TicketDetailDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            Category = ticket.Category,
            CreatedAt = ticket.CreatedAt,
            LastUpdatedAt = ticket.LastUpdatedAt,
            DueDate = ticket.DueDate,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.CustomerName,
            AssignedSupportId = ticket.AssignedSupportId,
            AssignedSupportName = ticket.AssignedSupportName,
            Department = ticket.Department is null ? null : new DepartmentDto
            {
                Id = ticket.Department.Id,
                Name = ticket.Department.Name
            },
            Tags = ticket.Tags
                .Where(relation => relation.Tag is not null)
                .Select(relation => new TagDto
                {
                    Id = relation.Tag!.Id,
                    Name = relation.Tag.Name,
                    Color = relation.Tag.Color
                })
                .ToList(),
            FirstResponseAt = ticket.FirstResponseAt,
            ResolvedAt = ticket.ResolvedAt,
            EscalationLevel = ticket.EscalationLevel,
            Replies = ticket.Replies.Select(reply => reply.ToDto()).ToList(),
            Attachments = ticket.Attachments.Select(attachment => attachment.ToDto()).ToList()
        };

        return detail;
    }

    public static TicketReplyDto ToDto(this TicketReply reply)
    {
        return new TicketReplyDto
        {
            Id = reply.Id,
            AuthorId = reply.AuthorId,
            AuthorName = reply.AuthorName,
            AuthorRole = reply.AuthorRole,
            Message = reply.Message,
            IsInternal = reply.IsInternal,
            TimeSpentMinutes = reply.TimeSpentMinutes,
            CreatedAt = reply.CreatedAt,
            Attachments = reply.Attachments.Select(attachment => attachment.ToDto()).ToList()
        };
    }

    public static TicketAttachmentDto ToDto(this TicketAttachment attachment)
    {
        return new TicketAttachmentDto
        {
            Id = attachment.Id,
            TicketId = attachment.TicketId,
            ReplyId = attachment.ReplyId,
            FileName = attachment.FileName,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            UploadedById = attachment.UploadedById,
            UploadedByName = attachment.UploadedByName,
            CreatedAt = attachment.CreatedAt
        };
    }

    public static TicketAuditDto ToDto(this TicketAuditEvent audit)
    {
        return new TicketAuditDto
        {
            Id = audit.Id,
            TicketId = audit.TicketId,
            ActorId = audit.ActorId,
            ActorName = audit.ActorName,
            Action = audit.Action,
            OldValue = audit.OldValue,
            NewValue = audit.NewValue,
            CreatedAt = audit.CreatedAt,
            IpAddress = audit.IpAddress
        };
    }

    public static UserProfileDto ToDto(this AppUser user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfileImageUrl = user.ProfileImageUrl,
            Phone = user.Phone,
            JobTitle = user.JobTitle,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.CompanyName,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled,
            TwoFactorEnabled = user.TwoFactorEnabled,
            PreferredLanguage = user.PreferredLanguage,
            DarkMode = user.DarkMode,
            Signature = user.Signature,
            IsActive = user.IsActive
        };
    }

    public static AdminUserDto ToAdminDto(this AppUser user)
    {
        return new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            JobTitle = user.JobTitle,
            Phone = user.Phone,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public static KnowledgeArticleSummaryDto ToSummaryDto(this KnowledgeArticle article)
    {
        return new KnowledgeArticleSummaryDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            AuthorName = article.AuthorName,
            PublishedAt = article.PublishedAt,
            ViewCount = article.ViewCount,
            CategoryName = article.Category?.Name
        };
    }

    public static KnowledgeArticleDetailDto ToDetailDto(this KnowledgeArticle article)
    {
        return new KnowledgeArticleDetailDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            AuthorName = article.AuthorName,
            PublishedAt = article.PublishedAt,
            ViewCount = article.ViewCount,
            CategoryName = article.Category?.Name,
            Content = article.Content,
            MetaKeywords = article.MetaKeywords,
            MetaDescription = article.MetaDescription,
            HelpfulCount = article.HelpfulCount,
            NotHelpfulCount = article.NotHelpfulCount
        };
    }
}
