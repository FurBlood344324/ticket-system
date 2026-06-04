namespace TicketSupport.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendAsync(string to, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
