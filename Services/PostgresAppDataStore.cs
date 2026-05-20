using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class PostgresAppDataStore : IAppDataStore
{
    private readonly ApplicationDbContext dbContext;

    public PostgresAppDataStore(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public List<AppUser> GetUsers()
    {
        return dbContext.Users.AsNoTracking().ToList();
    }

    public AppUser? FindUserByEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users.FirstOrDefault(user => user.Email == normalizedEmail);
    }

    public AppUser AddCustomer(RegisterViewModel model)
    {
        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(model.Password),
            Role = UserRole.Customer
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    public bool IsPasswordValid(AppUser user, string password)
    {
        return user.PasswordHash == PasswordHasher.Hash(password);
    }

    public List<SupportTicket> GetTickets()
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Replies)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToList();
    }

    public SupportTicket? FindTicket(int id)
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Replies)
            .FirstOrDefault(ticket => ticket.Id == id);
    }

    public SupportTicket AddTicket(CreateTicketViewModel model, AppUser customer)
    {
        var ticket = new SupportTicket
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CustomerId = customer.Id,
            CustomerName = customer.FullName
        };

        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();
        return ticket;
    }

    public void AssignTicket(int ticketId, AppUser supportUser)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId);
        if (ticket is null)
        {
            return;
        }

        ticket.AssignedSupportId = supportUser.Id;
        ticket.AssignedSupportName = supportUser.FullName;
        dbContext.SaveChanges();
    }

    public void AddReply(int ticketId, TicketReplyViewModel model, AppUser author)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId);
        if (ticket is null)
        {
            return;
        }

        ticket.Status = model.Status;
        dbContext.TicketReplies.Add(new TicketReply
        {
            TicketId = ticket.Id,
            AuthorId = author.Id,
            AuthorName = author.FullName,
            AuthorRole = author.Role,
            Message = model.Message.Trim()
        });

        dbContext.SaveChanges();
    }
}
