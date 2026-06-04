using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Controllers.Api.V1;

/// <summary>
/// Administrative API endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
[EnableRateLimiting("api")]
public class AdminController : ApiControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Lists users for administration.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AdminUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminUserDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<AdminUserDto>>.Ok(users.Select(user => user.ToAdminDto()).ToList()));
    }

    /// <summary>
    /// Updates a user as an administrator.
    /// </summary>
    [HttpPut("users/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateUser(int id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<AdminUserDto>();
        }

        var user = await dbContext.Users
            .Include(item => item.Department)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound(ApiResponse<AdminUserDto>.Fail("User not found."));
        }

        user.FullName = request.FullName.Trim();
        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(user).Reference(item => item.Department).LoadAsync(cancellationToken);
        return Ok(ApiResponse<AdminUserDto>.Ok(user.ToAdminDto(), "User updated."));
    }

    /// <summary>
    /// Returns high-level admin statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<AdminStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminStatsDto>>> GetStats(CancellationToken cancellationToken)
    {
        var stats = new AdminStatsDto
        {
            TotalUsers = await dbContext.Users.CountAsync(cancellationToken),
            ActiveUsers = await dbContext.Users.CountAsync(user => user.IsActive, cancellationToken),
            TotalTickets = await dbContext.Tickets.CountAsync(ticket => !ticket.IsDeleted, cancellationToken),
            OpenTickets = await dbContext.Tickets.CountAsync(ticket => !ticket.IsDeleted && ticket.Status == TicketStatus.Open, cancellationToken),
            ClosedTickets = await dbContext.Tickets.CountAsync(ticket => !ticket.IsDeleted && ticket.Status == TicketStatus.Closed, cancellationToken),
            UnassignedTickets = await dbContext.Tickets.CountAsync(ticket => !ticket.IsDeleted && ticket.AssignedSupportId == null, cancellationToken),
            PublishedKnowledgeArticles = await dbContext.KnowledgeArticles.CountAsync(article => article.IsPublished, cancellationToken)
        };

        return Ok(ApiResponse<AdminStatsDto>.Ok(stats));
    }
}
