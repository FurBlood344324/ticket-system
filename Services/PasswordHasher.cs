using Microsoft.Extensions.Configuration;

namespace TicketSupport.Services;

public class PasswordHasher
{
    private const int WorkFactor = 12;
    private readonly string pepper;

    public PasswordHasher(IConfiguration configuration)
    {
        pepper = configuration["Security:PasswordPepper"]?.Trim()
            ?? throw new InvalidOperationException("Security:PasswordPepper ayari bulunamadi.");

        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException("Security:PasswordPepper bos olamaz.");
        }
    }

    public string Hash(string password)
    {
        var saltedPassword = password + pepper;
        var salt = BCrypt.Net.BCrypt.GenerateSalt(WorkFactor, 'b');
        return BCrypt.Net.BCrypt.HashPassword(saltedPassword, salt);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password + pepper, hash);
    }
}
