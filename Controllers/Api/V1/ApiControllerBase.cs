using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Models;

namespace TicketSupport.Controllers.Api.V1;

public abstract class ApiControllerBase : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    protected UserRole? CurrentUserRole
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    protected string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    protected void WritePaginationHeader(PaginationMetadata pagination)
    {
        Response.Headers["X-Pagination"] = JsonSerializer.Serialize(pagination);
    }

    protected ActionResult<ApiResponse<T>> ValidationProblemResponse<T>()
    {
        var errors = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Validation error" : error.ErrorMessage)
            .Distinct()
            .ToArray();

        return BadRequest(ApiResponse<T>.Fail("Validation failed.", errors));
    }
}
