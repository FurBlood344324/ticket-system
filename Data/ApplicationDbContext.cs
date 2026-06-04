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

    public override int SaveChanges()
    {
        UpdateTicketAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTicketAuditFields();
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
            entity.HasOne(user => user.Department)
                .WithMany()
                .HasForeignKey(user => user.DepartmentId)
                .IsRequired(false);
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
            entity.Ignore(ticket => ticket.IsOverdue);
            entity.HasOne(ticket => ticket.Department)
                .WithMany()
                .HasForeignKey(ticket => ticket.DepartmentId)
                .IsRequired(false);
            entity.HasMany(ticket => ticket.Replies)
                .WithOne()
                .HasForeignKey(reply => reply.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.Tags)
                .WithOne(relation => relation.Ticket)
                .HasForeignKey(relation => relation.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketReply>(entity =>
        {
            entity.HasKey(reply => reply.Id);
            entity.Property(reply => reply.AuthorName).HasMaxLength(80).IsRequired();
            entity.Property(reply => reply.Message).HasMaxLength(1000).IsRequired();
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
    }

    private void UpdateTicketAuditFields()
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
    }
}
