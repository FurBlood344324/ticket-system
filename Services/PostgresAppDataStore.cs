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
            .Where(ticket => !ticket.IsDeleted)
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
            .Where(ticket => !ticket.IsDeleted)
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
            .FirstOrDefault(ticket => ticket.Id == id && !ticket.IsDeleted);
    }

    public SupportTicket? FindTicketDetails(int id)
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Replies.OrderBy(reply => reply.CreatedAt))
            .ThenInclude(reply => reply.Attachments)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .Include(ticket => ticket.Attachments)
            .Include(ticket => ticket.TimeEntries.OrderBy(entry => entry.CreatedAt))
            .Include(ticket => ticket.AuditEvents.OrderBy(audit => audit.CreatedAt))
            .FirstOrDefault(ticket => ticket.Id == id && !ticket.IsDeleted);
    }

    public TicketAttachment? FindAttachment(int id)
    {
        return dbContext.TicketAttachments
            .AsNoTracking()
            .Include(attachment => attachment.Reply)
            .Include(attachment => attachment.Ticket)
            .FirstOrDefault(attachment => attachment.Id == id);
    }

    public List<TicketTemplate> GetActiveTicketTemplates()
    {
        return dbContext.TicketTemplates
            .AsNoTracking()
            .Where(template => template.IsActive)
            .OrderBy(template => template.Title)
            .ToList();
    }

    public SlaPolicy? GetSlaPolicy(TicketPriority priority)
    {
        return dbContext.SlaPolicies
            .AsNoTracking()
            .FirstOrDefault(policy => policy.Priority == priority);
    }

    public SupportTicket AddTicket(CreateTicketViewModel model, AppUser customer, IReadOnlyCollection<TicketAttachment> attachments)
    {
        var selectedTemplate = model.SelectedTemplateId.HasValue
            ? dbContext.TicketTemplates.FirstOrDefault(template => template.Id == model.SelectedTemplateId.Value && template.IsActive)
            : null;

        var ticket = new SupportTicket
        {
            Title = ResolveTicketTitle(model, selectedTemplate),
            Description = ResolveTicketDescription(model, selectedTemplate),
            Priority = ResolveTicketPriority(model, selectedTemplate),
            Category = ResolveTicketCategory(model, selectedTemplate),
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            DepartmentId = selectedTemplate?.DepartmentId
        };

        dbContext.Tickets.Add(ticket);
        dbContext.SaveChanges();

        if (attachments.Count > 0)
        {
            foreach (var attachment in attachments)
            {
                attachment.TicketId = ticket.Id;
            }

            dbContext.TicketAttachments.AddRange(attachments);
        }

        if (selectedTemplate is not null)
        {
            selectedTemplate.UsageCount += 1;
        }

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = customer.Id,
            ActorName = customer.FullName,
            Action = "Talep oluşturuldu",
            NewValue = $"{ticket.Priority} / {ticket.Category}"
        });

        dbContext.SaveChanges();
        return ticket;
    }

    public void AssignTicket(int ticketId, AppUser supportUser)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId && !ticket.IsDeleted);
        if (ticket is null)
        {
            return;
        }

        var previousAssignee = ticket.AssignedSupportName;
        ticket.AssignedSupportId = supportUser.Id;
        ticket.AssignedSupportName = supportUser.FullName;
        ticket.LastUpdatedAt = DateTime.UtcNow;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = supportUser.Id,
            ActorName = supportUser.FullName,
            Action = "Talep üstlenildi",
            OldValue = string.IsNullOrWhiteSpace(previousAssignee) ? "Atanmamış" : previousAssignee,
            NewValue = supportUser.FullName
        });

        dbContext.SaveChanges();
    }

    public void AddReply(int ticketId, TicketReplyViewModel model, AppUser author, IReadOnlyCollection<TicketAttachment> attachments)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId && !ticket.IsDeleted);
        if (ticket is null)
        {
            return;
        }

        var previousStatus = ticket.Status;
        ticket.Status = model.Status;
        ticket.FirstResponseAt ??= DateTime.UtcNow;
        ticket.ResolvedAt = model.Status is TicketStatus.Solved or TicketStatus.Closed
            ? DateTime.UtcNow
            : null;
        var reply = new TicketReply
        {
            TicketId = ticket.Id,
            AuthorId = author.Id,
            AuthorName = author.FullName,
            AuthorRole = author.Role,
            Message = model.Message.Trim(),
            IsInternal = author.Role is UserRole.Support or UserRole.Admin && model.IsInternal,
            TimeSpentMinutes = author.Role is UserRole.Support or UserRole.Admin ? model.TimeSpentMinutes : null
        };

        dbContext.TicketReplies.Add(reply);
        dbContext.SaveChanges();

        if (attachments.Count > 0)
        {
            foreach (var attachment in attachments)
            {
                attachment.TicketId = ticket.Id;
                attachment.ReplyId = reply.Id;
            }

            dbContext.TicketAttachments.AddRange(attachments);
        }

        if (reply.TimeSpentMinutes.GetValueOrDefault() > 0)
        {
            dbContext.TicketTimeEntries.Add(new TicketTimeEntry
            {
                TicketId = ticket.Id,
                UserId = author.Id,
                UserName = author.FullName,
                Minutes = reply.TimeSpentMinutes ?? 0,
                Description = $"Yanıt üzerinden zaman girişi: {TrimForAudit(reply.Message)}",
                ActivityType = TicketActivityType.Work,
                CreatedAt = reply.CreatedAt
            });
        }

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = author.Id,
            ActorName = author.FullName,
            Action = reply.IsInternal ? "Dahili not eklendi" : "Yanıt eklendi",
            NewValue = TrimForAudit(reply.Message)
        });

        if (previousStatus != ticket.Status)
        {
            dbContext.TicketAuditEvents.Add(new TicketAuditEvent
            {
                TicketId = ticket.Id,
                ActorId = author.Id,
                ActorName = author.FullName,
                Action = "Durum güncellendi",
                OldValue = previousStatus.ToString(),
                NewValue = ticket.Status.ToString()
            });
        }

        dbContext.SaveChanges();
    }

    public void AddTimeEntry(int ticketId, TicketTimeEntryViewModel model, AppUser user, string? auditAction = null)
    {
        var ticketExists = dbContext.Tickets.Any(ticket => ticket.Id == ticketId && !ticket.IsDeleted);
        if (!ticketExists)
        {
            return;
        }

        dbContext.TicketTimeEntries.Add(new TicketTimeEntry
        {
            TicketId = ticketId,
            UserId = user.Id,
            UserName = user.FullName,
            Minutes = model.Minutes,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ActivityType = model.ActivityType
        });

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticketId,
            ActorId = user.Id,
            ActorName = user.FullName,
            Action = string.IsNullOrWhiteSpace(auditAction) ? "Zaman girişi eklendi" : auditAction,
            NewValue = $"{model.Minutes} dk / {model.ActivityType}"
        });

        dbContext.SaveChanges();
    }

    public void UpdateTicket(int ticketId, TicketEditViewModel model, AppUser actor)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId && !ticket.IsDeleted);
        if (ticket is null)
        {
            return;
        }

        var previousSnapshot = $"{ticket.Title} | {ticket.Priority} | {ticket.Category}";

        ticket.Title = model.Title.Trim();
        ticket.Description = model.Description.Trim();
        ticket.Priority = model.Priority;
        ticket.Category = model.Category;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = actor.Id,
            ActorName = actor.FullName,
            Action = "Talep bilgileri güncellendi",
            OldValue = previousSnapshot,
            NewValue = $"{ticket.Title} | {ticket.Priority} | {ticket.Category}"
        });

        dbContext.SaveChanges();
    }

    public void UpdateStatus(int ticketId, TicketStatus status, AppUser actor)
    {
        var ticket = dbContext.Tickets.FirstOrDefault(ticket => ticket.Id == ticketId && !ticket.IsDeleted);
        if (ticket is null || ticket.Status == status)
        {
            return;
        }

        var previousStatus = ticket.Status;
        ticket.Status = status;
        ticket.FirstResponseAt ??= DateTime.UtcNow;
        ticket.ResolvedAt = status is TicketStatus.Solved or TicketStatus.Closed
            ? DateTime.UtcNow
            : null;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = actor.Id,
            ActorName = actor.FullName,
            Action = "Durum güncellendi",
            OldValue = previousStatus.ToString(),
            NewValue = status.ToString()
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

    private static string ResolveTicketTitle(CreateTicketViewModel model, TicketTemplate? template)
    {
        if (!string.IsNullOrWhiteSpace(model.Title))
        {
            return model.Title.Trim();
        }

        return template?.Title.Trim() ?? string.Empty;
    }

    private static string ResolveTicketDescription(CreateTicketViewModel model, TicketTemplate? template)
    {
        if (!string.IsNullOrWhiteSpace(model.Description))
        {
            return model.Description.Trim();
        }

        return template?.Content.Trim() ?? string.Empty;
    }

    private static TicketPriority ResolveTicketPriority(CreateTicketViewModel model, TicketTemplate? template)
    {
        return model.Priority != default ? model.Priority : template?.Priority ?? TicketPriority.Medium;
    }

    private static TicketCategory ResolveTicketCategory(CreateTicketViewModel model, TicketTemplate? template)
    {
        return model.Category != default ? model.Category : template?.Category ?? TicketCategory.Other;
    }

    private static string TrimForAudit(string message)
    {
        var normalized = message.Trim().Replace(Environment.NewLine, " ");
        return normalized.Length <= 180 ? normalized : $"{normalized[..177]}...";
    }

    public DashboardViewModel GetDashboardData(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var sevenDaysAgo = now.Date.AddDays(-6);

        var query = dbContext.Tickets.AsNoTracking().AsQueryable();

        // Role-based filtering
        if (currentUser.Role == UserRole.Customer)
        {
            query = query.Where(t => t.CustomerId == currentUser.Id);
        }
        else if (currentUser.Role == UserRole.Support)
        {
            query = query.Where(t => t.AssignedSupportId == currentUser.Id || t.AssignedSupportId == null);
        }
        // Admin sees everything

        var tickets = query.ToList();

        var model = new DashboardViewModel
        {
            UserRole = currentUser.Role.ToString(),
            UserName = currentUser.FullName,
            TotalCount = tickets.Count,
            OpenCount = tickets.Count(t => t.Status == TicketStatus.Open),
            InProgressCount = tickets.Count(t => t.Status == TicketStatus.InProgress),
            SolvedCount = tickets.Count(t => t.Status == TicketStatus.Solved),
            ClosedCount = tickets.Count(t => t.Status == TicketStatus.Closed),
            UnassignedCount = tickets.Count(t => t.AssignedSupportId == null
                && t.Status != TicketStatus.Solved
                && t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Cancelled),
            TodayResolvedCount = tickets.Count(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == todayStart),
            SlaBreachCount = tickets.Count(t => t.DueDate.HasValue
                && t.DueDate.Value < now
                && t.Status != TicketStatus.Solved
                && t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Cancelled)
        };

        // Category distribution
        var categoryGroups = tickets
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key)
            .ToList();
        model.CategoryLabels = categoryGroups.Select(g => g.Key.ToString()).ToList();
        model.CategoryCounts = categoryGroups.Select(g => g.Count()).ToList();

        // Priority distribution
        var priorityGroups = tickets
            .GroupBy(t => t.Priority)
            .OrderBy(g => g.Key)
            .ToList();
        model.PriorityLabels = priorityGroups.Select(g => g.Key.ToString()).ToList();
        model.PriorityCounts = priorityGroups.Select(g => g.Count()).ToList();

        // Last 7 days trend
        for (var day = 0; day < 7; day++)
        {
            var date = sevenDaysAgo.AddDays(day);
            model.TrendLabels.Add(date.ToString("dd MMM"));
            model.TrendCounts.Add(tickets.Count(t => t.CreatedAt.Date == date));
        }

        // SLA breach tickets
        model.SlaBreachTickets = tickets
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value < now
                && t.Status != TicketStatus.Solved
                && t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Select(t => new SlaBreachItem
            {
                TicketId = t.Id,
                Title = t.Title,
                CustomerName = t.CustomerName,
                AssignedTo = t.AssignedSupportName,
                Priority = t.Priority.ToString(),
                DueDate = t.DueDate,
                Status = t.Status.ToString()
            })
            .ToList();

        // Recent updates (last 10 updated tickets)
        model.RecentUpdates = tickets
            .OrderByDescending(t => t.LastUpdatedAt)
            .Take(10)
            .Select(t => new RecentUpdateItem
            {
                TicketId = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                UpdatedBy = t.AssignedSupportName ?? t.CustomerName,
                UpdatedAt = t.LastUpdatedAt
            })
            .ToList();

        return model;
    }
}
