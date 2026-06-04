using Microsoft.EntityFrameworkCore;
using TicketSupport.Models;

namespace TicketSupport.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<TicketTag> TicketTags => Set<TicketTag>();
    public DbSet<TicketTagRelation> TicketTagRelations => Set<TicketTagRelation>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<CustomerOrganization> CustomerOrganizations => Set<CustomerOrganization>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<CannedResponse> CannedResponses => Set<CannedResponse>();
    public DbSet<CustomerSatisfaction> CustomerSatisfactions => Set<CustomerSatisfaction>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketAuditEvent> TicketAuditEvents => Set<TicketAuditEvent>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<KnowledgeCategory> KnowledgeCategories => Set<KnowledgeCategory>();
    public DbSet<RelatedArticle> RelatedArticles => Set<RelatedArticle>();
    public DbSet<TicketTemplate> TicketTemplates => Set<TicketTemplate>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<TicketTimeEntry> TicketTimeEntries => Set<TicketTimeEntry>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<RecurringTicket> RecurringTickets => Set<RecurringTicket>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(120).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(120).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();

            // Yeni alan konfigürasyonları
            entity.Property(user => user.ProfileImageUrl).HasMaxLength(500);
            entity.Property(user => user.Phone).HasMaxLength(20);
            entity.Property(user => user.JobTitle).HasMaxLength(100);
            entity.Property(user => user.TwoFactorSecretKey).HasMaxLength(100);
            entity.Property(user => user.PreferredLanguage).HasMaxLength(10).HasDefaultValue("tr");
            entity.Property(user => user.EmailNotificationsEnabled).HasDefaultValue(true);
            entity.Property(user => user.PushNotificationsEnabled).HasDefaultValue(true);
            entity.Property(user => user.TwoFactorEnabled).HasDefaultValue(false);
            entity.Property(user => user.DarkMode).HasDefaultValue(false);
            entity.Property(user => user.IsActive).HasDefaultValue(true);
            entity.Property(user => user.Signature).HasMaxLength(500);

            entity.HasOne(user => user.Department)
                .WithMany()
                .HasForeignKey(user => user.DepartmentId)
                .IsRequired(false);

            entity.HasOne(user => user.Organization)
                .WithMany(org => org.Members)
                .HasForeignKey(user => user.OrganizationId)
                .IsRequired(false);
        });

        modelBuilder.Entity<CustomerOrganization>(entity =>
        {
            entity.HasKey(org => org.Id);
            entity.Property(org => org.CompanyName).HasMaxLength(120).IsRequired();
            entity.Property(org => org.TaxNumber).HasMaxLength(20);
            entity.Property(org => org.Phone).HasMaxLength(20);
            entity.Property(org => org.Address).HasMaxLength(250);
            entity.Property(org => org.IsActive).HasDefaultValue(true);
            entity.HasIndex(org => org.CompanyName);
        });

        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(pref => pref.Id);
            entity.Property(pref => pref.EventType).HasMaxLength(60).IsRequired();
            entity.Property(pref => pref.EmailEnabled).HasDefaultValue(true);
            entity.Property(pref => pref.PushEnabled).HasDefaultValue(true);
            entity.HasIndex(pref => new { pref.UserId, pref.EventType }).IsUnique();
            entity.HasOne(pref => pref.User)
                .WithMany(user => user.NotificationPreferences)
                .HasForeignKey(pref => pref.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Title).HasMaxLength(120).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(1200).IsRequired();
            entity.Property(ticket => ticket.CustomerName).HasMaxLength(80).IsRequired();
            entity.Property(ticket => ticket.AssignedSupportName).HasMaxLength(80);
            entity.Property(ticket => ticket.Priority).HasDefaultValue(TicketPriority.Medium);
            entity.Property(ticket => ticket.Category).HasDefaultValue(TicketCategory.Other);
            entity.Property(ticket => ticket.LastUpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(ticket => ticket.EscalationLevel).HasDefaultValue(0);
            entity.Property(ticket => ticket.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(ticket => ticket.IsDeleted);
            entity.Ignore(ticket => ticket.IsOverdue);
            entity.HasOne(ticket => ticket.Department)
                .WithMany()
                .HasForeignKey(ticket => ticket.DepartmentId)
                .IsRequired(false);
            entity.HasMany(ticket => ticket.Replies)
                .WithOne(reply => reply.Ticket)
                .HasForeignKey(reply => reply.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.SatisfactionEntries)
                .WithOne(satisfaction => satisfaction.Ticket)
                .HasForeignKey(satisfaction => satisfaction.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.Attachments)
                .WithOne(attachment => attachment.Ticket)
                .HasForeignKey(attachment => attachment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.Tags)
                .WithOne(relation => relation.Ticket)
                .HasForeignKey(relation => relation.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.TimeEntries)
                .WithOne()
                .HasForeignKey(entry => entry.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.AuditEvents)
                .WithOne()
                .HasForeignKey(audit => audit.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketReply>(entity =>
        {
            entity.HasKey(reply => reply.Id);
            entity.Property(reply => reply.AuthorName).HasMaxLength(80).IsRequired();
            entity.Property(reply => reply.Message).HasMaxLength(1000).IsRequired();
            entity.Property(reply => reply.IsInternal).HasDefaultValue(false);
            entity.Property(reply => reply.IsAiSuggested).HasDefaultValue(false);
            entity.HasOne(reply => reply.Template)
                .WithMany(template => template.Replies)
                .HasForeignKey(reply => reply.TemplateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(reply => reply.Attachments)
                .WithOne(attachment => attachment.Reply)
                .HasForeignKey(attachment => attachment.ReplyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CannedResponse>(entity =>
        {
            entity.HasKey(response => response.Id);
            entity.Property(response => response.Title).HasMaxLength(120).IsRequired();
            entity.Property(response => response.Category).HasMaxLength(80).IsRequired();
            entity.Property(response => response.Content).HasMaxLength(4000).IsRequired();
            entity.Property(response => response.CreatedByName).HasMaxLength(80).IsRequired();
            entity.Property(response => response.IsShared).HasDefaultValue(false);
            entity.Property(response => response.UsageCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<CustomerSatisfaction>(entity =>
        {
            entity.HasKey(satisfaction => satisfaction.Id);
            entity.Property(satisfaction => satisfaction.Comment).HasMaxLength(1000);
            entity.Property(satisfaction => satisfaction.Rating).IsRequired();
            entity.Property(satisfaction => satisfaction.IsVisibleToCustomer).HasDefaultValue(true);
            entity.ToTable(table => table.HasCheckConstraint("CK_CustomerSatisfaction_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.HasKey(attachment => attachment.Id);
            entity.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(attachment => attachment.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(attachment => attachment.UploadedByName).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Name).HasMaxLength(80).IsRequired();
            entity.Property(department => department.Description).HasMaxLength(200);
            entity.HasIndex(department => department.Name).IsUnique();
        });

        modelBuilder.Entity<TicketTag>(entity =>
        {
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Name).HasMaxLength(60).IsRequired();
            entity.Property(tag => tag.Color).HasMaxLength(20).IsRequired();
            entity.HasIndex(tag => tag.Name).IsUnique();
        });

        modelBuilder.Entity<TicketTagRelation>(entity =>
        {
            entity.HasKey(relation => new { relation.TicketId, relation.TagId });
            entity.HasOne(relation => relation.Ticket)
                .WithMany(ticket => ticket.Tags)
                .HasForeignKey(relation => relation.TicketId);
            entity.HasOne(relation => relation.Tag)
                .WithMany(tag => tag.TicketRelations)
                .HasForeignKey(relation => relation.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.Property(policy => policy.Priority).IsRequired();
            entity.Property(policy => policy.ResponseTimeMinutes).IsRequired();
            entity.Property(policy => policy.ResolutionTimeMinutes).IsRequired();
            entity.Property(policy => policy.BusinessHoursOnly).IsRequired();
        });

        // ──────────────────────────────────────────────
        // YENİ ENTITY KONFİGÜRASYONLARI
        // ──────────────────────────────────────────────

        modelBuilder.Entity<TicketAuditEvent>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.ActorName).HasMaxLength(80).IsRequired();
            entity.Property(audit => audit.Action).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.IpAddress).HasMaxLength(45);
            entity.HasIndex(audit => audit.TicketId);
            entity.HasIndex(audit => audit.CreatedAt);
            entity.HasIndex(audit => audit.ActorId);
        });

        modelBuilder.Entity<KnowledgeCategory>(entity =>
        {
            entity.HasKey(cat => cat.Id);
            entity.Property(cat => cat.Name).HasMaxLength(100).IsRequired();
            entity.Property(cat => cat.Description).HasMaxLength(300);
            entity.Property(cat => cat.SortOrder).HasDefaultValue(0);
            entity.Property(cat => cat.IsActive).HasDefaultValue(true);
            entity.HasIndex(cat => cat.Name).IsUnique();
        });

        modelBuilder.Entity<KnowledgeArticle>(entity =>
        {
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Title).HasMaxLength(200).IsRequired();
            entity.Property(article => article.Slug).HasMaxLength(200).IsRequired();
            entity.Property(article => article.Content).HasMaxLength(5000).IsRequired();
            entity.Property(article => article.AuthorName).HasMaxLength(80).IsRequired();
            entity.Property(article => article.MetaKeywords).HasMaxLength(300);
            entity.Property(article => article.MetaDescription).HasMaxLength(300);
            entity.Property(article => article.IsPublished).HasDefaultValue(false);
            entity.Property(article => article.ViewCount).HasDefaultValue(0);
            entity.Property(article => article.HelpfulCount).HasDefaultValue(0);
            entity.Property(article => article.NotHelpfulCount).HasDefaultValue(0);
            entity.HasIndex(article => article.Slug).IsUnique();
            entity.HasIndex(article => article.CreatedAt);
            entity.HasIndex(article => article.ViewCount);
            entity.HasOne(article => article.Category)
                .WithMany(cat => cat.Articles)
                .HasForeignKey(article => article.KnowledgeCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RelatedArticle>(entity =>
        {
            entity.HasKey(relation => new { relation.ArticleId, relation.RelatedArticleId });
            entity.HasOne(relation => relation.Article)
                .WithMany()
                .HasForeignKey(relation => relation.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(relation => relation.Related)
                .WithMany()
                .HasForeignKey(relation => relation.RelatedArticleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketTemplate>(entity =>
        {
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Title).HasMaxLength(150).IsRequired();
            entity.Property(template => template.Description).HasMaxLength(500);
            entity.Property(template => template.Content).HasMaxLength(5000).IsRequired();
            entity.Property(template => template.CreatedByName).HasMaxLength(80).IsRequired();
            entity.Property(template => template.IsActive).HasDefaultValue(true);
            entity.Property(template => template.UsageCount).HasDefaultValue(0);
            entity.HasIndex(template => template.Category);
            entity.HasIndex(template => template.IsActive);
            entity.HasOne(template => template.Department)
                .WithMany()
                .HasForeignKey(template => template.DepartmentId)
                .IsRequired(false);
        });

        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(webhook => webhook.Id);
            entity.Property(webhook => webhook.Name).HasMaxLength(100).IsRequired();
            entity.Property(webhook => webhook.Url).HasMaxLength(500).IsRequired();
            entity.Property(webhook => webhook.Secret).HasMaxLength(500).IsRequired();
            entity.Property(webhook => webhook.EventTypes).HasMaxLength(500).IsRequired();
            entity.Property(webhook => webhook.IsActive).HasDefaultValue(true);
            entity.Property(webhook => webhook.FailedCount).HasDefaultValue(0);
            entity.HasIndex(webhook => webhook.IsActive);
        });

        modelBuilder.Entity<TicketTimeEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.UserName).HasMaxLength(80).IsRequired();
            entity.Property(entry => entry.Description).HasMaxLength(500);
            entity.HasIndex(entry => entry.TicketId);
            entity.HasIndex(entry => entry.UserId);
            entity.HasIndex(entry => entry.CreatedAt);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
            entity.Property(notification => notification.Url).HasMaxLength(500);
            entity.Property(notification => notification.IsRead).HasDefaultValue(false);
            entity.HasIndex(notification => notification.UserId);
            entity.HasIndex(notification => notification.IsRead);
            entity.HasIndex(notification => notification.CreatedAt);
            entity.HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringTicket>(entity =>
        {
            entity.HasKey(recurring => recurring.Id);
            entity.Property(recurring => recurring.Title).HasMaxLength(150).IsRequired();
            entity.Property(recurring => recurring.Description).HasMaxLength(1200).IsRequired();
            entity.Property(recurring => recurring.CronExpression).HasMaxLength(100).IsRequired();
            entity.Property(recurring => recurring.IsActive).HasDefaultValue(true);
            entity.HasIndex(recurring => recurring.IsActive);
            entity.HasIndex(recurring => recurring.NextRunAt);
            entity.HasOne(recurring => recurring.Department)
                .WithMany()
                .HasForeignKey(recurring => recurring.DepartmentId)
                .IsRequired(false);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(apiKey => apiKey.Id);
            entity.Property(apiKey => apiKey.Key).HasMaxLength(200).IsRequired();
            entity.Property(apiKey => apiKey.Name).HasMaxLength(100).IsRequired();
            entity.Property(apiKey => apiKey.Scopes).HasMaxLength(500).IsRequired();
            entity.Property(apiKey => apiKey.IsActive).HasDefaultValue(true);
            entity.HasIndex(apiKey => apiKey.Key).IsUnique();
            entity.HasIndex(apiKey => apiKey.UserId);
            entity.HasIndex(apiKey => apiKey.IsActive);
            entity.HasOne(apiKey => apiKey.User)
                .WithMany()
                .HasForeignKey(apiKey => apiKey.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<SupportTicket>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.LastUpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<KnowledgeArticle>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
