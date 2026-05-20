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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(120).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Title).HasMaxLength(120).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(1200).IsRequired();
            entity.Property(ticket => ticket.CustomerName).HasMaxLength(80).IsRequired();
            entity.Property(ticket => ticket.AssignedSupportName).HasMaxLength(80);
            entity.HasMany(ticket => ticket.Replies)
                .WithOne()
                .HasForeignKey(reply => reply.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketReply>(entity =>
        {
            entity.HasKey(reply => reply.Id);
            entity.Property(reply => reply.AuthorName).HasMaxLength(80).IsRequired();
            entity.Property(reply => reply.Message).HasMaxLength(1000).IsRequired();
        });
    }
}
