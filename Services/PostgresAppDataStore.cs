using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class PostgresAppDataStore : IAppDataStore
{
    private readonly ApplicationDbContext dbContext;
    private readonly PasswordHasher passwordHasher;
    private readonly TicketQueryService ticketQueryService;

    public PostgresAppDataStore(
        ApplicationDbContext dbContext,
        PasswordHasher passwordHasher,
        TicketQueryService ticketQueryService)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.ticketQueryService = ticketQueryService;
    }

    public List<AppUser> GetUsers()
    {
        return dbContext.Users.AsNoTracking().Include(user => user.Department).ToList();
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
            PasswordHash = passwordHasher.Hash(model.Password),
            Role = UserRole.Customer
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    public bool IsPasswordValid(AppUser user, string password)
    {
        if (IsBcryptHash(user.PasswordHash))
        {
            return passwordHasher.Verify(password, user.PasswordHash);
        }

        if (!IsLegacySha256Hash(user.PasswordHash) || !VerifyLegacySha256(password, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = passwordHasher.Hash(password);
        dbContext.SaveChanges();
        return true;
    }

    private static bool IsBcryptHash(string hash)
    {
        return !string.IsNullOrWhiteSpace(hash)
            && hash.Length == 60
            && hash.StartsWith("$2", StringComparison.Ordinal);
    }

    private static bool IsLegacySha256Hash(string hash)
    {
        return !string.IsNullOrWhiteSpace(hash)
            && hash.Length == 64
            && hash.All(IsHexCharacter);
    }

    private static bool VerifyLegacySha256(string password, string hash)
    {
        var expectedHashBytes = Convert.FromHexString(hash);
        var actualHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }

    private static bool IsHexCharacter(char value)
    {
        return value is >= '0' and <= '9'
            or >= 'A' and <= 'F'
            or >= 'a' and <= 'f';
    }

    public List<SupportTicket> GetTickets()
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Replies)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToList();
    }

    public TicketFilterViewModel GetFilteredTickets(TicketFilterViewModel filter, AppUser currentUser)
    {
        var normalizedFilter = NormalizeFilter(filter);
        int? forcedCustomerId = currentUser.Role == UserRole.Customer ? currentUser.Id : null;

        var baseQuery = dbContext.Tickets
            .AsNoTracking()
            .AsQueryable();

        baseQuery = ticketQueryService.ApplyFilters(baseQuery, normalizedFilter, forcedCustomerId);

        normalizedFilter.TotalCount = baseQuery.Count();
        normalizedFilter.TotalPages = normalizedFilter.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(normalizedFilter.TotalCount / (double)normalizedFilter.PageSize);

        if (normalizedFilter.TotalPages > 0 && normalizedFilter.Page > normalizedFilter.TotalPages)
        {
            normalizedFilter.Page = normalizedFilter.TotalPages;
        }

        normalizedFilter.Tickets = ticketQueryService
            .ApplySorting(baseQuery, normalizedFilter.SortBy)
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .Skip((normalizedFilter.Page - 1) * normalizedFilter.PageSize)
            .Take(normalizedFilter.PageSize)
            .ToList();

        normalizedFilter.AvailableDepartments = dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name)
            .ToList();

        normalizedFilter.AvailableTags = dbContext.TicketTags
            .AsNoTracking()
            .OrderBy(tag => tag.Name)
            .ToList();

        var activeUsers = dbContext.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .Where(user => user.IsActive)
            .OrderBy(user => user.FullName)
            .ToList();

        normalizedFilter.AvailableAgents = activeUsers
            .Where(user => user.Role is UserRole.Support or UserRole.Admin)
            .ToList();
        normalizedFilter.AvailableCustomers = activeUsers
            .Where(user => user.Role == UserRole.Customer)
            .ToList();

        return normalizedFilter;
    }

    public SupportTicket? FindTicket(int id)
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Replies)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .FirstOrDefault(ticket => ticket.Id == id);
    }

    public SupportTicket AddTicket(CreateTicketViewModel model, AppUser customer)
    {
        var ticket = new SupportTicket
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Priority = model.Priority,
            Category = model.Category,
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
        ticket.LastUpdatedAt = DateTime.UtcNow;
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
        ticket.FirstResponseAt ??= DateTime.UtcNow;
        ticket.ResolvedAt = model.Status is TicketStatus.Solved or TicketStatus.Closed
            ? DateTime.UtcNow
            : null;
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

    private static TicketFilterViewModel NormalizeFilter(TicketFilterViewModel filter)
    {
        var pageSizeOptions = new[] { 20, 50, 100 };
        var normalizedPageSize = pageSizeOptions.Contains(filter.PageSize) ? filter.PageSize : 20;

        return new TicketFilterViewModel
        {
            Search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            Status = filter.Status,
            Priority = filter.Priority,
            Category = filter.Category,
            DepartmentId = filter.DepartmentId,
            TagIds = filter.TagIds.Where(tagId => tagId > 0).Distinct().ToList(),
            AssignedToId = filter.AssignedToId,
            CustomerId = filter.CustomerId,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            DueDateFrom = filter.DueDateFrom,
            DueDateTo = filter.DueDateTo,
            IsOverdue = filter.IsOverdue,
            IsUnassigned = filter.IsUnassigned,
            SortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "newest" : filter.SortBy.Trim().ToLowerInvariant(),
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = normalizedPageSize,
            PageSizeOptions = pageSizeOptions.ToList()
        };
    }
}
