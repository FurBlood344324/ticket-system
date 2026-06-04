using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketSupport.Data;

namespace TicketSupport.Services;

public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";

    private readonly ApplicationDbContext dbContext;
    private readonly PasswordHasher passwordHasher;

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext dbContext,
        PasswordHasher passwordHasher) : base(options, logger, encoder)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedKey = headerValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.Fail("API key is missing.");
        }

        var apiKeys = await dbContext.ApiKeys
            .Include(apiKey => apiKey.User)
            .Where(apiKey => apiKey.IsActive && apiKey.User != null && apiKey.User.IsActive)
            .ToListAsync();

        var apiKey = apiKeys.FirstOrDefault(candidate =>
            (candidate.ExpiresAt is null || candidate.ExpiresAt > DateTime.UtcNow)
            && passwordHasher.Verify(providedKey, candidate.Key));

        if (apiKey?.User is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.User.Id.ToString()),
            new Claim(ClaimTypes.Name, apiKey.User.FullName),
            new Claim(ClaimTypes.Email, apiKey.User.Email),
            new Claim(ClaimTypes.Role, apiKey.User.Role.ToString()),
            new Claim("auth_scheme", SchemeName),
            new Claim("api_key_id", apiKey.Id.ToString())
        };

        if (!string.IsNullOrWhiteSpace(apiKey.Scopes))
        {
            claims.Add(new Claim("scopes", apiKey.Scopes));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
