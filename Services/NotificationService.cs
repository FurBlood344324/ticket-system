using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class NotificationService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<NotificationService> logger;

    public NotificationService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    // ──────────────────────────────────────────────
    // Public event methods — fire-and-forget
    // ──────────────────────────────────────────────

    public void TicketCreated(SupportTicket ticket)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyTicketCreatedAsync(ticket);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TicketCreated notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void TicketAssigned(SupportTicket ticket, AppUser assignee)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyTicketAssignedAsync(ticket, assignee);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TicketAssigned notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void NewReply(SupportTicket ticket, TicketReply reply)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyNewReplyAsync(ticket, reply);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in NewReply notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void StatusChanged(SupportTicket ticket, TicketStatus oldStatus, TicketStatus newStatus)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyStatusChangedAsync(ticket, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in StatusChanged notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void SlaBreached(SupportTicket ticket)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifySlaBreachedAsync(ticket);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SlaBreached notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void TicketOverdue(SupportTicket ticket)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyTicketOverdueAsync(ticket);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TicketOverdue notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void SurveyRequested(SupportTicket ticket)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifySurveyRequestedAsync(ticket);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SurveyRequested notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    public void MentionReceived(SupportTicket ticket, AppUser mentionedUser)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyMentionReceivedAsync(ticket, mentionedUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MentionReceived notification for ticket #{TicketId}", ticket.Id);
            }
        });
    }

    // ──────────────────────────────────────────────
    // In-app notification helpers (non-fire-and-forget)
    // ──────────────────────────────────────────────

    public async Task<List<UserNotification>> GetNotificationsAsync(int userId, int page = 1, int pageSize = 20)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkReadAsync(int notificationId, int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var notification = await db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is not null)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unreadNotifications = await db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        if (unreadNotifications.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    // ──────────────────────────────────────────────
    // Private notification implementations
    // ──────────────────────────────────────────────

    private async Task NotifyTicketCreatedAsync(SupportTicket ticket)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        // Notify all support/agent users about new ticket
        var agents = await db.Users
            .Where(u => u.IsActive && u.EmailNotificationsEnabled
                && (u.Role == UserRole.Support || u.Role == UserRole.Admin))
            .ToListAsync();

        foreach (var agent in agents)
        {
            // In-app notification
            db.UserNotifications.Add(new UserNotification
            {
                UserId = agent.Id,
                Title = "Yeni Destek Talebi",
                Message = $"#{ticket.Id} - {ticket.Title} başlıklı yeni bir talep oluşturuldu.",
                Url = ticketUrl,
                NotificationType = NotificationType.TicketAssigned
            });

            // Email notification
            if (ShouldSendEmail(agent, "TicketCreated"))
            {
                var html = await templateEngine.RenderAsync("TicketCreated.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = agent.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["CustomerName"] = ticket.CustomerName,
                    ["Priority"] = ticket.Priority.ToString(),
                    ["Category"] = ticket.Category.ToString(),
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(agent.Email, agent.FullName,
                        $"Yeni Talep #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyTicketAssignedAsync(SupportTicket ticket, AppUser assignee)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        // Notify the assignee
        if (assignee.IsActive && assignee.EmailNotificationsEnabled)
        {
            db.UserNotifications.Add(new UserNotification
            {
                UserId = assignee.Id,
                Title = "Talep Size Atandı",
                Message = $"#{ticket.Id} - {ticket.Title} talebi size atandı.",
                Url = ticketUrl,
                NotificationType = NotificationType.TicketAssigned
            });

            if (ShouldSendEmail(assignee, "TicketAssigned"))
            {
                var html = await templateEngine.RenderAsync("TicketAssigned.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = assignee.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["CustomerName"] = ticket.CustomerName,
                    ["Priority"] = ticket.Priority.ToString(),
                    ["Category"] = ticket.Category.ToString(),
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(assignee.Email, assignee.FullName,
                        $"Talep Ataması #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        // Notify the customer
        var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.CustomerId);
        if (customer is not null && customer.IsActive && customer.EmailNotificationsEnabled)
        {
            db.UserNotifications.Add(new UserNotification
            {
                UserId = customer.Id,
                Title = "Talebiniz Atandı",
                Message = $"#{ticket.Id} - Talebiniz {assignee.FullName} adlı kişiye atandı.",
                Url = ticketUrl,
                NotificationType = NotificationType.TicketAssigned
            });

            if (ShouldSendEmail(customer, "TicketAssigned"))
            {
                var html = await templateEngine.RenderAsync("TicketAssigned.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = customer.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["CustomerName"] = ticket.CustomerName,
                    ["Priority"] = ticket.Priority.ToString(),
                    ["Category"] = ticket.Category.ToString(),
                    ["AssigneeName"] = assignee.FullName,
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(customer.Email, customer.FullName,
                        $"Talebiniz Atandı #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyNewReplyAsync(SupportTicket ticket, TicketReply reply)
    {
        if (reply.IsInternal)
        {
            await NotifyInternalNoteAsync(ticket, reply);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";
        var replyExcerpt = reply.Message.Length > 150 ? reply.Message[..147] + "..." : reply.Message;

        // If reply is from support/agent, notify the customer
        if (reply.AuthorRole is UserRole.Support or UserRole.Admin)
        {
            var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.CustomerId);
            if (customer is not null && customer.IsActive && customer.EmailNotificationsEnabled)
            {
                db.UserNotifications.Add(new UserNotification
                {
                    UserId = customer.Id,
                    Title = "Talebinize Yeni Yanıt",
                    Message = $"#{ticket.Id} - Talebinize {reply.AuthorName} tarafından yeni bir yanıt eklendi: \"{replyExcerpt}\"",
                    Url = ticketUrl,
                    NotificationType = NotificationType.TicketReplied
                });

                if (ShouldSendEmail(customer, "NewReply"))
                {
                    var html = await templateEngine.RenderAsync("NewReply.cshtml", new Dictionary<string, string>
                    {
                        ["FullName"] = customer.FullName,
                        ["TicketId"] = ticket.Id.ToString(),
                        ["TicketTitle"] = ticket.Title,
                        ["ReplyAuthorName"] = reply.AuthorName,
                        ["ReplyMessage"] = reply.Message,
                        ["TicketUrl"] = ticketUrl,
                        ["BaseUrl"] = baseUrl,
                        ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                    });

                    if (!string.IsNullOrEmpty(html))
                    {
                        await emailService.SendAsync(customer.Email, customer.FullName,
                            $"Yeni Yanıt #{ticket.Id} - {ticket.Title}", html);
                    }
                }
            }
        }
        else
        {
            // Reply is from customer, notify assigned support
            var assignee = ticket.AssignedSupportId.HasValue
                ? await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.AssignedSupportId.Value)
                : null;

            // Also notify all support users if unassigned
            var agents = new List<AppUser>();
            if (assignee is not null && assignee.IsActive)
            {
                agents.Add(assignee);
            }
            else
            {
                agents = await db.Users
                    .Where(u => u.IsActive && (u.Role == UserRole.Support || u.Role == UserRole.Admin))
                    .ToListAsync();
            }

            foreach (var agent in agents)
            {
                if (!agent.EmailNotificationsEnabled) continue;

                db.UserNotifications.Add(new UserNotification
                {
                    UserId = agent.Id,
                    Title = "Müşteri Yanıtı",
                    Message = $"#{ticket.Id} - {ticket.CustomerName} talebe yeni bir yanıt ekledi: \"{replyExcerpt}\"",
                    Url = ticketUrl,
                    NotificationType = NotificationType.TicketReplied
                });

                if (ShouldSendEmail(agent, "NewReply"))
                {
                    var html = await templateEngine.RenderAsync("NewReply.cshtml", new Dictionary<string, string>
                    {
                        ["FullName"] = agent.FullName,
                        ["TicketId"] = ticket.Id.ToString(),
                        ["TicketTitle"] = ticket.Title,
                        ["ReplyAuthorName"] = reply.AuthorName,
                        ["ReplyMessage"] = reply.Message,
                        ["TicketUrl"] = ticketUrl,
                        ["BaseUrl"] = baseUrl,
                        ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                    });

                    if (!string.IsNullOrEmpty(html))
                    {
                        await emailService.SendAsync(agent.Email, agent.FullName,
                            $"Müşteri Yanıtı #{ticket.Id} - {ticket.Title}", html);
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyInternalNoteAsync(SupportTicket ticket, TicketReply reply)
    {
        // Internal notes are only visible to support/agents — notify only them, not the customer
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var agents = await db.Users
            .Where(u => u.IsActive && (u.Role == UserRole.Support || u.Role == UserRole.Admin))
            .ToListAsync();

        var noteExcerpt = reply.Message.Length > 150 ? reply.Message[..147] + "..." : reply.Message;
        var baseUrl = scope.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection("Smtp")["BaseUrl"] ?? "https://localhost:5001";

        foreach (var agent in agents)
        {
            if (agent.Id == reply.AuthorId) continue; // Don't notify the author

            db.UserNotifications.Add(new UserNotification
            {
                UserId = agent.Id,
                Title = "Dahili Not Eklendi",
                Message = $"#{ticket.Id} - {ticket.Title} talebine {reply.AuthorName} tarafından dahili not eklendi: \"{noteExcerpt}\"",
                Url = $"{baseUrl.TrimEnd('/')}/Tickets/Details/{ticket.Id}",
                NotificationType = NotificationType.TicketUpdated
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyStatusChangedAsync(SupportTicket ticket, TicketStatus oldStatus, TicketStatus newStatus)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";
        var statusText = GetStatusText(newStatus);

        // Notify the customer
        var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.CustomerId);
        if (customer is not null && customer.IsActive && customer.EmailNotificationsEnabled)
        {
            var notificationType = newStatus switch
            {
                TicketStatus.Solved => NotificationType.TicketResolved,
                TicketStatus.Closed => NotificationType.TicketClosed,
                _ => NotificationType.TicketUpdated
            };

            db.UserNotifications.Add(new UserNotification
            {
                UserId = customer.Id,
                Title = "Talep Durumu Güncellendi",
                Message = $"#{ticket.Id} - Talebinizin durumu \"{statusText}\" olarak güncellendi.",
                Url = ticketUrl,
                NotificationType = notificationType
            });

            if (ShouldSendEmail(customer, "StatusChanged"))
            {
                var html = await templateEngine.RenderAsync("StatusChanged.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = customer.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["OldStatus"] = GetStatusText(oldStatus),
                    ["NewStatus"] = statusText,
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(customer.Email, customer.FullName,
                        $"Durum Güncellendi #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        // Notify assigned support
        if (ticket.AssignedSupportId.HasValue)
        {
            var assignee = await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.AssignedSupportId.Value);
            if (assignee is not null && assignee.IsActive && assignee.EmailNotificationsEnabled)
            {
                db.UserNotifications.Add(new UserNotification
                {
                    UserId = assignee.Id,
                    Title = "Talep Durumu Değişti",
                    Message = $"#{ticket.Id} - {ticket.Title} talebinin durumu \"{statusText}\" olarak güncellendi.",
                    Url = ticketUrl,
                    NotificationType = NotificationType.TicketUpdated
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifySlaBreachedAsync(SupportTicket ticket)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        // Notify assigned support
        var assignee = ticket.AssignedSupportId.HasValue
            ? await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.AssignedSupportId.Value)
            : null;

        var agentsToNotify = new List<AppUser>();
        if (assignee is not null && assignee.IsActive)
        {
            agentsToNotify.Add(assignee);
        }

        // Always notify admins about SLA breaches
        var admins = await db.Users
            .Where(u => u.IsActive && u.Role == UserRole.Admin)
            .ToListAsync();

        foreach (var admin in admins)
        {
            if (!agentsToNotify.Any(a => a.Id == admin.Id))
            {
                agentsToNotify.Add(admin);
            }
        }

        foreach (var agent in agentsToNotify)
        {
            if (!agent.EmailNotificationsEnabled) continue;

            db.UserNotifications.Add(new UserNotification
            {
                UserId = agent.Id,
                Title = "SLA İhlali!",
                Message = $"#{ticket.Id} - {ticket.Title} talebi için SLA süresi aşıldı! Öncelik: {ticket.Priority}",
                Url = ticketUrl,
                NotificationType = NotificationType.SlaBreach
            });

            if (ShouldSendEmail(agent, "SlaWarning"))
            {
                var html = await templateEngine.RenderAsync("SlaWarning.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = agent.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["CustomerName"] = ticket.CustomerName,
                    ["Priority"] = ticket.Priority.ToString(),
                    ["DueDate"] = ticket.DueDate?.ToString("dd.MM.yyyy HH:mm") ?? "Bilinmiyor",
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(agent.Email, agent.FullName,
                        $"⚠️ SLA İhlali #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyTicketOverdueAsync(SupportTicket ticket)
    {
        // Similar to SLA breach but slightly different messaging
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        var assignee = ticket.AssignedSupportId.HasValue
            ? await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.AssignedSupportId.Value)
            : null;

        if (assignee is not null && assignee.IsActive && assignee.EmailNotificationsEnabled)
        {
            db.UserNotifications.Add(new UserNotification
            {
                UserId = assignee.Id,
                Title = "Talep Gecikti",
                Message = $"#{ticket.Id} - {ticket.Title} talebi için son tarih aşıldı.",
                Url = ticketUrl,
                NotificationType = NotificationType.SlaBreach
            });

            if (ShouldSendEmail(assignee, "SlaWarning"))
            {
                var html = await templateEngine.RenderAsync("SlaWarning.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = assignee.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["CustomerName"] = ticket.CustomerName,
                    ["Priority"] = ticket.Priority.ToString(),
                    ["DueDate"] = ticket.DueDate?.ToString("dd.MM.yyyy HH:mm") ?? "Bilinmiyor",
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(assignee.Email, assignee.FullName,
                        $"⚠️ Geciken Talep #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifySurveyRequestedAsync(SupportTicket ticket)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == ticket.CustomerId);
        if (customer is not null && customer.IsActive && customer.EmailNotificationsEnabled)
        {
            db.UserNotifications.Add(new UserNotification
            {
                UserId = customer.Id,
                Title = "Memnuniyet Anketi",
                Message = $"#{ticket.Id} - {ticket.Title} talebi için memnuniyet anketini doldurmak ister misiniz?",
                Url = ticketUrl,
                NotificationType = NotificationType.TicketResolved
            });

            if (ShouldSendEmail(customer, "SatisfactionSurvey"))
            {
                var html = await templateEngine.RenderAsync("SatisfactionSurvey.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = customer.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(customer.Email, customer.FullName,
                        $"Memnuniyet Anketi #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task NotifyMentionReceivedAsync(SupportTicket ticket, AppUser mentionedUser)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<EmailTemplateEngine>();

        var baseUrl = templateEngine.GetBaseUrl();
        var ticketUrl = $"{baseUrl}/Tickets/Details/{ticket.Id}";

        if (mentionedUser.IsActive && mentionedUser.EmailNotificationsEnabled)
        {
            db.UserNotifications.Add(new UserNotification
            {
                UserId = mentionedUser.Id,
                Title = "Bahsedildiniz",
                Message = $"#{ticket.Id} - {ticket.Title} talebinde bahsedildiniz.",
                Url = ticketUrl,
                NotificationType = NotificationType.System
            });

            if (ShouldSendEmail(mentionedUser, "MentionNotification"))
            {
                var html = await templateEngine.RenderAsync("MentionNotification.cshtml", new Dictionary<string, string>
                {
                    ["FullName"] = mentionedUser.FullName,
                    ["TicketId"] = ticket.Id.ToString(),
                    ["TicketTitle"] = ticket.Title,
                    ["TicketUrl"] = ticketUrl,
                    ["BaseUrl"] = baseUrl,
                    ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
                });

                if (!string.IsNullOrEmpty(html))
                {
                    await emailService.SendAsync(mentionedUser.Email, mentionedUser.FullName,
                        $"Bahsedildiniz #{ticket.Id} - {ticket.Title}", html);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static bool ShouldSendEmail(AppUser user, string eventType)
    {
        if (!user.EmailNotificationsEnabled) return false;

        var pref = user.NotificationPreferences
            .FirstOrDefault(p => p.EventType == eventType);

        return pref?.EmailEnabled ?? true; // Default: enabled
    }

    private static string GetStatusText(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Açık",
        TicketStatus.Solved => "Çözüldü",
        TicketStatus.Closed => "Kapandı",
        TicketStatus.InProgress => "İşlemde",
        TicketStatus.WaitingCustomer => "Müşteri Bekleniyor",
        TicketStatus.Cancelled => "İptal",
        _ => status.ToString()
    };
}
