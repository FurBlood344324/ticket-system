using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers.Api.V1;

/// <summary>
/// Ticket operations for the REST API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/tickets")]
[Authorize]
[EnableRateLimiting("api")]
public class TicketsController : ApiControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly TicketQueryService ticketQueryService;
    private readonly FileAttachmentService fileAttachmentService;

    public TicketsController(
        ApplicationDbContext dbContext,
        TicketQueryService ticketQueryService,
        FileAttachmentService fileAttachmentService)
    {
        this.dbContext = dbContext;
        this.ticketQueryService = ticketQueryService;
        this.fileAttachmentService = fileAttachmentService;
    }

    /// <summary>
    /// Lists tickets with filtering, sorting and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketSummaryDto>>>> GetTickets([FromQuery] TicketListQueryRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyCollection<TicketSummaryDto>>.Fail("User could not be resolved."));
        }

        var filter = new TicketFilterViewModel
        {
            Search = request.Search,
            Status = request.Status,
            Priority = request.Priority,
            Category = request.Category,
            DepartmentId = request.DepartmentId,
            TagIds = request.TagIds,
            AssignedToId = request.AssignedToId,
            CustomerId = request.CustomerId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            DueDateFrom = request.DueDateFrom,
            DueDateTo = request.DueDateTo,
            IsOverdue = request.IsOverdue,
            IsUnassigned = request.IsUnassigned,
            SortBy = request.SortBy,
            Page = request.Page,
            PageSize = request.PageSize
        };

        int? forcedCustomerId = currentUser.Role == UserRole.Customer ? currentUser.Id : null;
        var baseQuery = dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => !ticket.IsDeleted);

        baseQuery = ticketQueryService.ApplyFilters(baseQuery, filter, forcedCustomerId);
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var normalizedPageSize = new[] { 20, 50, 100 }.Contains(request.PageSize) ? request.PageSize : 20;
        var page = request.Page < 1 ? 1 : request.Page;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var tickets = await ticketQueryService
            .ApplySorting(baseQuery, request.SortBy)
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .Skip((page - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var pagination = new PaginationMetadata
        {
            Page = page,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        WritePaginationHeader(pagination);
        return Ok(ApiResponse<IReadOnlyCollection<TicketSummaryDto>>.Ok(tickets.Select(ticket => ticket.ToSummaryDto()).ToList(), pagination: pagination));
    }

    /// <summary>
    /// Gets a ticket with replies and attachments.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> GetTicket(int id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        var ticket = await LoadTicketDetailQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketDetailDto>.Fail("Ticket not found."));
        }

        if (!CanAccessTicket(ticket, currentUser))
        {
            return Forbid();
        }

        return Ok(ApiResponse<TicketDetailDto>.Ok(ticket.ToDetailDto()));
    }

    /// <summary>
    /// Creates a new ticket for the authenticated customer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<TicketDetailDto>();
        }

        var currentUser = await GetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        var selectedTemplate = request.SelectedTemplateId.HasValue
            ? await dbContext.TicketTemplates.FirstOrDefaultAsync(template => template.Id == request.SelectedTemplateId.Value && template.IsActive, cancellationToken)
            : null;

        var ticket = new SupportTicket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            Category = request.Category,
            CustomerId = currentUser.Id,
            CustomerName = currentUser.FullName,
            DepartmentId = selectedTemplate?.DepartmentId
        };

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (selectedTemplate is not null)
        {
            selectedTemplate.UsageCount += 1;
        }

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Talep oluşturuldu",
            NewValue = $"{ticket.Priority} / {ticket.Category}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var createdTicket = await LoadTicketDetailQuery().FirstAsync(item => item.Id == ticket.Id, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ApiResponse<TicketDetailDto>.Ok(createdTicket.ToDetailDto(), "Ticket created."));
    }

    /// <summary>
    /// Updates a ticket.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> UpdateTicket(int id, [FromBody] UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<TicketDetailDto>();
        }

        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketDetailDto>.Fail("Ticket not found."));
        }

        var previousSnapshot = $"{ticket.Title} | {ticket.Priority} | {ticket.Category}";
        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Priority = request.Priority;
        ticket.Category = request.Category;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Talep bilgileri güncellendi",
            OldValue = previousSnapshot,
            NewValue = $"{ticket.Title} | {ticket.Priority} | {ticket.Category}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var updatedTicket = await LoadTicketDetailQuery().FirstAsync(item => item.Id == ticket.Id, cancellationToken);
        return Ok(ApiResponse<TicketDetailDto>.Ok(updatedTicket.ToDetailDto(), "Ticket updated."));
    }

    /// <summary>
    /// Soft deletes a ticket.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTicket(int id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<object>.Fail("Ticket not found."));
        }

        ticket.IsDeleted = true;
        ticket.DeletedAt = DateTime.UtcNow;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Talep silindi",
            NewValue = $"Deleted at {ticket.DeletedAt:O}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Ticket soft deleted."));
    }

    /// <summary>
    /// Adds a reply to a ticket.
    /// </summary>
    [HttpPost("{id:int}/reply")]
    [ProducesResponseType(typeof(ApiResponse<TicketReplyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketReplyDto>>> AddReply(int id, [FromBody] AddTicketReplyRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<TicketReplyDto>();
        }

        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketReplyDto>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketReplyDto>.Fail("Ticket not found."));
        }

        if (!CanAccessTicket(ticket, currentUser))
        {
            return Forbid();
        }

        if (currentUser.Role == UserRole.Customer && ticket.Status == TicketStatus.Closed)
        {
            return BadRequest(ApiResponse<TicketReplyDto>.Fail("Closed tickets cannot receive customer replies."));
        }

        if (currentUser.Role == UserRole.Customer)
        {
            request.Status = TicketStatus.Open;
            request.IsInternal = false;
            request.TimeSpentMinutes = null;
        }

        var previousStatus = ticket.Status;
        ticket.Status = request.Status;
        ticket.FirstResponseAt ??= DateTime.UtcNow;
        ticket.ResolvedAt = request.Status is TicketStatus.Solved or TicketStatus.Closed ? DateTime.UtcNow : null;

        var reply = new TicketReply
        {
            TicketId = ticket.Id,
            AuthorId = currentUser.Id,
            AuthorName = currentUser.FullName,
            AuthorRole = currentUser.Role,
            Message = request.Message.Trim(),
            IsInternal = currentUser.Role is UserRole.Support or UserRole.Admin && request.IsInternal,
            TimeSpentMinutes = currentUser.Role is UserRole.Support or UserRole.Admin ? request.TimeSpentMinutes : null
        };

        dbContext.TicketReplies.Add(reply);
        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = reply.IsInternal ? "Dahili not eklendi" : "Yanıt eklendi",
            NewValue = request.Message.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        if (previousStatus != ticket.Status)
        {
            dbContext.TicketAuditEvents.Add(new TicketAuditEvent
            {
                TicketId = ticket.Id,
                ActorId = currentUser.Id,
                ActorName = currentUser.FullName,
                Action = "Durum güncellendi",
                OldValue = previousStatus.ToString(),
                NewValue = ticket.Status.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<TicketReplyDto>.Ok(reply.ToDto(), "Reply added."));
    }

    /// <summary>
    /// Changes the status of a ticket.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> ChangeStatus(int id, [FromBody] UpdateTicketStatusRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketDetailDto>.Fail("Ticket not found."));
        }

        var previousStatus = ticket.Status;
        ticket.Status = request.Status;
        ticket.FirstResponseAt ??= DateTime.UtcNow;
        ticket.ResolvedAt = request.Status is TicketStatus.Solved or TicketStatus.Closed ? DateTime.UtcNow : null;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Durum güncellendi",
            OldValue = previousStatus.ToString(),
            NewValue = request.Status.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var updatedTicket = await LoadTicketDetailQuery().FirstAsync(item => item.Id == ticket.Id, cancellationToken);
        return Ok(ApiResponse<TicketDetailDto>.Ok(updatedTicket.ToDetailDto(), "Status updated."));
    }

    /// <summary>
    /// Assigns the ticket to the current support user.
    /// </summary>
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> AssignToSelf(int id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketDetailDto>.Fail("Ticket not found."));
        }

        var previousAssignee = ticket.AssignedSupportName;
        ticket.AssignedSupportId = currentUser.Id;
        ticket.AssignedSupportName = currentUser.FullName;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Talep üstlenildi",
            OldValue = string.IsNullOrWhiteSpace(previousAssignee) ? "Atanmamış" : previousAssignee,
            NewValue = currentUser.FullName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var updatedTicket = await LoadTicketDetailQuery().FirstAsync(item => item.Id == ticket.Id, cancellationToken);
        return Ok(ApiResponse<TicketDetailDto>.Ok(updatedTicket.ToDetailDto(), "Ticket assigned."));
    }

    /// <summary>
    /// Changes the priority of a ticket.
    /// </summary>
    [HttpPut("{id:int}/priority")]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> ChangePriority(int id, [FromBody] UpdateTicketPriorityRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await dbContext.Tickets.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<TicketDetailDto>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<TicketDetailDto>.Fail("Ticket not found."));
        }

        var previousPriority = ticket.Priority;
        ticket.Priority = request.Priority;

        dbContext.TicketAuditEvents.Add(new TicketAuditEvent
        {
            TicketId = ticket.Id,
            ActorId = currentUser.Id,
            ActorName = currentUser.FullName,
            Action = "Öncelik güncellendi",
            OldValue = previousPriority.ToString(),
            NewValue = request.Priority.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var updatedTicket = await LoadTicketDetailQuery().FirstAsync(item => item.Id == ticket.Id, cancellationToken);
        return Ok(ApiResponse<TicketDetailDto>.Ok(updatedTicket.ToDetailDto(), "Priority updated."));
    }

    /// <summary>
    /// Lists attachments for a ticket.
    /// </summary>
    [HttpGet("{id:int}/attachments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>>> GetAttachments(int id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await LoadTicketDetailQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Fail("Ticket not found."));
        }

        if (!CanAccessTicket(ticket, currentUser))
        {
            return Forbid();
        }

        return Ok(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Ok(ticket.Attachments.Select(attachment => attachment.ToDto()).ToList()));
    }

    /// <summary>
    /// Uploads attachments for a ticket.
    /// </summary>
    [HttpPost("{id:int}/attachments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>>> UploadAttachments(int id, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await LoadTicketDetailQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Fail("Ticket not found."));
        }

        if (!CanAccessTicket(ticket, currentUser))
        {
            return Forbid();
        }

        try
        {
            var uploaded = await fileAttachmentService.UploadAsync(files, currentUser, ticket.Id, cancellationToken: cancellationToken);
            dbContext.TicketAttachments.AddRange(uploaded);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Ok(uploaded.Select(attachment => attachment.ToDto()).ToList(), "Attachments uploaded."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(ApiResponse<IReadOnlyCollection<TicketAttachmentDto>>.Fail(exception.Message));
        }
    }

    /// <summary>
    /// Lists audit log entries for a ticket.
    /// </summary>
    [HttpGet("{id:int}/audit")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketAuditDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketAuditDto>>>> GetAuditLog(int id, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var ticket = await LoadTicketDetailQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyCollection<TicketAuditDto>>.Fail("User could not be resolved."));
        }

        if (ticket is null)
        {
            return NotFound(ApiResponse<IReadOnlyCollection<TicketAuditDto>>.Fail("Ticket not found."));
        }

        if (!CanAccessTicket(ticket, currentUser))
        {
            return Forbid();
        }

        return Ok(ApiResponse<IReadOnlyCollection<TicketAuditDto>>.Ok(ticket.AuditEvents.Select(audit => audit.ToDto()).ToList()));
    }

    private IQueryable<SupportTicket> LoadTicketDetailQuery()
    {
        return dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => !ticket.IsDeleted)
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Tags)
            .ThenInclude(relation => relation.Tag)
            .Include(ticket => ticket.Replies.OrderBy(reply => reply.CreatedAt))
            .ThenInclude(reply => reply.Attachments)
            .Include(ticket => ticket.Attachments)
            .Include(ticket => ticket.AuditEvents.OrderBy(audit => audit.CreatedAt));
    }

    private async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
        {
            return null;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .Include(user => user.Organization)
            .FirstOrDefaultAsync(user => user.Id == CurrentUserId.Value && user.IsActive, cancellationToken);
    }

    private static bool CanAccessTicket(SupportTicket ticket, AppUser currentUser)
    {
        return currentUser.Role is UserRole.Support or UserRole.Admin
            || ticket.CustomerId == currentUser.Id;
    }
}
