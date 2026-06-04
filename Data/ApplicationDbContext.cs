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
            entity.HasOne(ticket => ticket.Department)
                .WithMany()
                .HasForeignKey(ticket => ticket.DepartmentId)
                .IsRequired(false);
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

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Name).HasMaxLength(80).IsRequired();
            entity.Property(department => department.Description).HasMaxLength(200);
            entity.HasIndex(department => department.Name).IsUnique();
        });
    }
}
