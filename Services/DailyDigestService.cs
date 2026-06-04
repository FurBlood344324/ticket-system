using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class DailyDigestService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DailyDigestService> logger;

    public DailyDigestService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyDigestService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Run at 08:00 UTC every day (11:00 TR time)
            var nextRun = now.Date.AddHours(8);
            if (now >= nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            logger.LogInformation("DailyDigestService next run scheduled at {NextRun} (in {Delay})", nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await SendDailyDigestAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DailyDigestService scheduled run");
            }
        }
    }

    public async Task SendDailyDigestAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);

        // Get all tickets
        var allTickets = await db.Tickets.AsNoTracking().ToListAsync();

        var openTickets = allTickets.Where(t => t.Status != TicketStatus.Closed
            && t.Status != TicketStatus.Solved
            && t.Status != TicketStatus.Cancelled).ToList();

        var slaBreaches = openTickets
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < now)
            .OrderBy(t => t.DueDate)
            .ToList();

        var closedToday = allTickets
            .Where(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == todayStart)
            .ToList();

        var createdToday = allTickets
            .Where(t => t.CreatedAt.Date == todayStart)
            .ToList();

        var unassigned = openTickets
            .Where(t => t.AssignedSupportId == null)
            .ToList();

        // Average resolution time for tickets resolved today
        string avgResolutionTime;
        if (closedToday.Count > 0)
        {
            var avgHours = closedToday
                .Select(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                .Average();
            avgResolutionTime = avgHours >= 24
                ? $"{avgHours / 24:F1} gün"
                : $"{avgHours:F1} saat";
        }
        else
        {
            avgResolutionTime = "—";
        }

        var baseUrl = templateEngine.GetBaseUrl();
        var reportDate = now.ToString("dd MMMM yyyy");

        // Build SLA breach items for the template
        var slaBreachItems = slaBreaches.Select(t => new Dictionary<string, string>
        {
            ["SlaTicketInfo"] = $"#{t.Id} - {t.Title} | Müşteri: {t.CustomerName} | Öncelik: {t.Priority} | Son Tarih: {t.DueDate:dd.MM.yyyy HH:mm}"
        }).ToList();

        var variables = new Dictionary<string, string>
        {
            ["FullName"] = string.Empty, // Will be replaced per recipient
            ["ReportDate"] = reportDate,
            ["OpenCount"] = openTickets.Count.ToString(),
            ["SlaBreachCount"] = slaBreaches.Count.ToString(),
            ["ClosedTodayCount"] = closedToday.Count.ToString(),
            ["TotalOpenCount"] = openTickets.Count.ToString(),
            ["UnassignedCount"] = unassigned.Count.ToString(),
            ["CreatedTodayCount"] = createdToday.Count.ToString(),
            ["AvgResolutionTime"] = avgResolutionTime,
            ["BaseUrl"] = baseUrl,
            ["CurrentYear"] = now.Year.ToString()
        };

        // Get all admin and support users who have email notifications enabled
        var recipients = await db.Users
            .Where(u => u.IsActive && u.EmailNotificationsEnabled
                && (u.Role == UserRole.Admin || u.Role == UserRole.Support))
            .ToListAsync();

        logger.LogInformation(
            "Sending daily digest to {RecipientCount} recipients. Open: {Open}, SLA breaches: {Sla}, Closed today: {Closed}",
            recipients.Count, openTickets.Count, slaBreaches.Count, closedToday.Count);

        foreach (var recipient in recipients)
        {
            variables["FullName"] = recipient.FullName;

            var html = await templateEngine.RenderLoopAsync(
                "DailyDigest.cshtml",
                variables,
                "SlaBreaches",
                slaBreachItems);

            if (!string.IsNullOrEmpty(html))
            {
                await emailService.SendAsync(
                    recipient.Email,
                    recipient.FullName,
                    $"Günlük Özet — {reportDate}",
                    html);
            }
        }
    }
}
