using System.Net;
using System.Net.Mail;

namespace TicketSupport.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<SmtpEmailService> logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        return SendAsync(to, to, subject, htmlBody, cancellationToken);
    }

    public async Task SendAsync(string to, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var smtpSection = configuration.GetSection("Smtp");
        var host = smtpSection["Host"] ?? "localhost";
        var port = int.TryParse(smtpSection["Port"], out var parsedPort) ? parsedPort : 1025;
        var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var parsedSsl) && parsedSsl;
        var user = smtpSection["User"] ?? string.Empty;
        var pass = smtpSection["Pass"] ?? string.Empty;
        var from = smtpSection["From"] ?? "noreply@ticketsupport.local";
        var fromName = smtpSection["FromName"] ?? "Ticket Destek Sistemi";

        var maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                if (!string.IsNullOrWhiteSpace(user))
                {
                    smtpClient.Credentials = new NetworkCredential(user, pass);
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(from, fromName, System.Text.Encoding.UTF8),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };

                mailMessage.To.Add(new MailAddress(to, toName));

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                logger.LogInformation(
                    "Email sent successfully to {To} with subject '{Subject}' on attempt {Attempt}",
                    to, subject, attempt);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    ex,
                    "Failed to send email to {To} on attempt {Attempt}/{MaxRetries}. Retrying...",
                    to, attempt, maxRetries);

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send email to {To} after {MaxRetries} attempts. Subject: '{Subject}'",
                    to, maxRetries, subject);
            }
        }
    }
}
