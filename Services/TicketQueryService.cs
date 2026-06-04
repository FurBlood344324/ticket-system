using Microsoft.EntityFrameworkCore;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class TicketQueryService
{
    public IQueryable<SupportTicket> ApplyFilters(
        IQueryable<SupportTicket> query,
        TicketFilterViewModel filter,
        int? forcedCustomerId = null)
    {
        var effectiveCustomerId = forcedCustomerId ?? filter.CustomerId;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = $"%{filter.Search.Trim()}%";
            query = query.Where(ticket =>
                EF.Functions.ILike(ticket.Title, searchTerm)
                || EF.Functions.ILike(ticket.Description, searchTerm));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == filter.Priority.Value);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(ticket => ticket.Category == filter.Category.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(ticket => ticket.DepartmentId == filter.DepartmentId.Value);
        }

        if (effectiveCustomerId.HasValue)
        {
            query = query.Where(ticket => ticket.CustomerId == effectiveCustomerId.Value);
        }

        if (filter.AssignedToId.HasValue)
        {
            query = query.Where(ticket => ticket.AssignedSupportId == filter.AssignedToId.Value);
        }

        if (filter.TagIds.Count > 0)
        {
            foreach (var tagId in filter.TagIds.Distinct())
            {
                query = query.Where(ticket => ticket.Tags.Any(tag => tag.TagId == tagId));
            }
        }

        if (filter.FromDate.HasValue)
        {
            var fromDate = filter.FromDate.Value.Date;
            query = query.Where(ticket => ticket.CreatedAt >= fromDate);
        }

        if (filter.ToDate.HasValue)
        {
            var toDateExclusive = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(ticket => ticket.CreatedAt < toDateExclusive);
        }

        if (filter.DueDateFrom.HasValue)
        {
            var dueDateFrom = filter.DueDateFrom.Value.Date;
            query = query.Where(ticket => ticket.DueDate.HasValue && ticket.DueDate.Value >= dueDateFrom);
        }

        if (filter.DueDateTo.HasValue)
        {
            var dueDateToExclusive = filter.DueDateTo.Value.Date.AddDays(1);
            query = query.Where(ticket => ticket.DueDate.HasValue && ticket.DueDate.Value < dueDateToExclusive);
        }

        if (filter.IsOverdue)
        {
            var now = DateTime.UtcNow;
            query = query.Where(ticket =>
                ticket.DueDate.HasValue
                && ticket.DueDate.Value < now
                && ticket.Status != TicketStatus.Solved
                && ticket.Status != TicketStatus.Closed
                && ticket.Status != TicketStatus.Cancelled);
        }

        if (filter.IsUnassigned)
        {
            query = query.Where(ticket => ticket.AssignedSupportId == null);
        }

        return query;
    }

    public IQueryable<SupportTicket> ApplySorting(IQueryable<SupportTicket> query, string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(ticket => ticket.CreatedAt),
            "priority_desc" => query
                .OrderByDescending(ticket => ticket.Priority)
                .ThenByDescending(ticket => ticket.CreatedAt),
            "priority_asc" => query
                .OrderBy(ticket => ticket.Priority)
                .ThenByDescending(ticket => ticket.CreatedAt),
            "due_date" => query
                .OrderBy(ticket => ticket.DueDate.HasValue ? 0 : 1)
                .ThenBy(ticket => ticket.DueDate)
                .ThenByDescending(ticket => ticket.CreatedAt),
            "last_updated" => query.OrderByDescending(ticket => ticket.LastUpdatedAt),
            _ => query.OrderByDescending(ticket => ticket.CreatedAt)
        };
    }
}
