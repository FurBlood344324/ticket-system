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
/// Account endpoints for the authenticated user.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/account")]
[Authorize]
[EnableRateLimiting("api")]
public class AccountController : ApiControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly PasswordHasher passwordHasher;

    public AccountController(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Returns the current user profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMe(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("User could not be resolved."));
        }

        return Ok(ApiResponse<UserProfileDto>.Ok(user.ToDto()));
    }

    /// <summary>
    /// Updates the current user profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<UserProfileDto>();
        }

        var user = await dbContext.Users
            .Include(item => item.Department)
            .Include(item => item.Organization)
            .FirstOrDefaultAsync(item => item.Id == CurrentUserId && item.IsActive, cancellationToken);

        if (user is null)
        {
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("User could not be resolved."));
        }

        user.FullName = request.FullName.Trim();
        user.ProfileImageUrl = NormalizeNullable(request.ProfileImageUrl);
        user.Phone = NormalizeNullable(request.Phone);
        user.JobTitle = NormalizeNullable(request.JobTitle);
        user.PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "tr" : request.PreferredLanguage.Trim();
        user.Signature = NormalizeNullable(request.Signature);
        user.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        user.PushNotificationsEnabled = request.PushNotificationsEnabled;
        user.DarkMode = request.DarkMode;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<UserProfileDto>.Ok(user.ToDto(), "Profile updated."));
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblemResponse<object>();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == CurrentUserId && item.IsActive, cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("User could not be resolved."));
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password changed."));
    }

    private async Task<AppUser?> FindCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .Include(user => user.Organization)
            .FirstOrDefaultAsync(user => user.Id == CurrentUserId && user.IsActive, cancellationToken);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
