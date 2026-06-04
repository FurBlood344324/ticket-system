using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private const string TimerCacheKeyPrefix = "ticket-timer";

    private readonly IAppDataStore dataStore;
    private readonly FileAttachmentService fileAttachmentService;
    private readonly IMemoryCache memoryCache;

    public TicketsController(
        IAppDataStore dataStore,
        FileAttachmentService fileAttachmentService,
        IMemoryCache memoryCache)
    {
        this.dataStore = dataStore;
        this.fileAttachmentService = fileAttachmentService;
        this.memoryCache = memoryCache;
    }

    public IActionResult Index([FromQuery] TicketFilterViewModel filter)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null)
        {
            return Challenge();
        }

        return View(dataStore.GetFilteredTickets(filter, currentUser));
    }

    public IActionResult Details(int id)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicketDetails(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        return View(BuildDetailsViewModel(ticket, currentUser));
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    public IActionResult Create()
    {
        var model = new CreateTicketViewModel();
        PopulateCreateModel(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model, IFormFileCollection files, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null)
        {
            return Challenge();
        }

        ApplyTemplateDefaults(model);

        if (!ModelState.IsValid)
        {
            PopulateCreateModel(model);
            return View(model);
        }

        List<TicketAttachment> attachments;
        try
        {
            attachments = await fileAttachmentService.UploadAsync(files, currentUser, cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            PopulateCreateModel(model);
            return View(model);
        }

        try
        {
            var ticket = dataStore.AddTicket(model, currentUser, attachments);
            TempData["Message"] = "Destek talebiniz oluşturuldu.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
        catch
        {
            foreach (var attachment in attachments)
            {
                await fileAttachmentService.DeleteAsync(attachment);
            }

            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult Assign(int id)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null)
        {
            return Challenge();
        }

        dataStore.AssignTicket(id, currentUser);
        TempData["Message"] = "Talep üzerinize alındı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, TicketReplyViewModel model, IFormFileCollection files, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicketDetails(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        if (currentUser.Role == UserRole.Customer && ticket.Status == TicketStatus.Closed)
        {
            ModelState.AddModelError(string.Empty, "Kapanmış talebe cevap yazılamaz.");
        }

        if (currentUser.Role == UserRole.Customer)
        {
            model.Status = TicketStatus.Open;
            model.IsInternal = false;
            model.TimeSpentMinutes = null;
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Details), BuildDetailsViewModel(ticket, currentUser, replyModel: model));
        }

        List<TicketAttachment> attachments;
        try
        {
            attachments = await fileAttachmentService.UploadAsync(files, currentUser, ticketId: ticket.Id, cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(nameof(Details), BuildDetailsViewModel(ticket, currentUser, replyModel: model));
        }

        try
        {
            dataStore.AddReply(id, model, currentUser, attachments);
            TempData["Message"] = "Cevabınız kaydedildi.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch
        {
            foreach (var attachment in attachments)
            {
                await fileAttachmentService.DeleteAsync(attachment);
            }

            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult AddTimeEntry(int id, TicketTimeEntryViewModel model)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicketDetails(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Details), BuildDetailsViewModel(ticket, currentUser, timeEntryModel: model));
        }

        dataStore.AddTimeEntry(id, model, currentUser);
        TempData["Message"] = "Zaman girişi kaydedildi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditTicket(int id, TicketEditViewModel model)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicketDetails(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Details), BuildDetailsViewModel(ticket, currentUser, editModel: model));
        }

        dataStore.UpdateTicket(id, model, currentUser);
        TempData["Message"] = "Talep bilgileri güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int id, TicketStatus status)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicketDetails(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        dataStore.UpdateStatus(id, status, currentUser);
        return Json(new
        {
            ok = true,
            statusText = GetStatusText(status),
            badgeClass = GetStatusBadgeClass(status)
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult StartTimer(int id)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicket(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        var startedAt = DateTime.UtcNow;
        memoryCache.Set(GetTimerCacheKey(currentUser.Id, id), startedAt, TimeSpan.FromHours(8));

        return Json(new
        {
            ok = true,
            startedAt = startedAt.ToString("O")
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Support)},{nameof(UserRole.Admin)}")]
    [ValidateAntiForgeryToken]
    public IActionResult StopTimer(int id)
    {
        var currentUser = GetCurrentUser();
        var ticket = dataStore.FindTicket(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        if (!memoryCache.TryGetValue<DateTime>(GetTimerCacheKey(currentUser.Id, id), out var startedAt))
        {
            return Json(new { ok = false, message = "Çalışan bir sayaç bulunamadı." });
        }

        memoryCache.Remove(GetTimerCacheKey(currentUser.Id, id));

        var elapsed = DateTime.UtcNow - startedAt;
        var minutes = Math.Max(1, (int)Math.Ceiling(elapsed.TotalMinutes));
        var timeEntry = new TicketTimeEntryViewModel
        {
            Minutes = minutes,
            ActivityType = TicketActivityType.Work,
            Description = $"Zamanlayıcı ile kaydedildi ({startedAt:dd.MM.yyyy HH:mm} - {DateTime.UtcNow:dd.MM.yyyy HH:mm})"
        };

        dataStore.AddTimeEntry(id, timeEntry, currentUser, "Zamanlayıcı durduruldu");

        return Json(new
        {
            ok = true,
            minutes,
            totalLabel = $"{minutes} dk kaydedildi"
        });
    }

    public IActionResult DownloadAttachment(int id)
    {
        var currentUser = GetCurrentUser();
        var attachment = dataStore.FindAttachment(id);
        if (currentUser is null || attachment?.Ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(attachment.Ticket, currentUser))
        {
            return Forbid();
        }

        if (currentUser.Role == UserRole.Customer && attachment.Reply?.IsInternal == true)
        {
            return Forbid();
        }

        if (!System.IO.File.Exists(attachment.StoragePath))
        {
            return NotFound();
        }

        var fileStream = new FileStream(attachment.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, attachment.ContentType, attachment.OriginalFileName);
    }

    private AppUser? GetCurrentUser()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var id))
        {
            return null;
        }

        return dataStore.GetUsers().FirstOrDefault(user => user.Id == id);
    }

    private TicketDetailsViewModel BuildDetailsViewModel(
        SupportTicket ticket,
        AppUser currentUser,
        TicketReplyViewModel? replyModel = null,
        TicketEditViewModel? editModel = null,
        TicketTimeEntryViewModel? timeEntryModel = null)
    {
        var isAgent = currentUser.Role is UserRole.Support or UserRole.Admin;
        var visibleReplies = isAgent
            ? ticket.Replies
            : ticket.Replies.Where(reply => !reply.IsInternal).ToList();

        var visibleAttachments = isAgent
            ? ticket.Attachments
            : ticket.Attachments.Where(attachment => attachment.ReplyId is null || visibleReplies.Any(reply => reply.Id == attachment.ReplyId)).ToList();

        var replyLookup = visibleReplies.ToDictionary(
            reply => reply.Id,
            reply => new TicketReply
            {
                Id = reply.Id,
                TicketId = reply.TicketId,
                AuthorId = reply.AuthorId,
                AuthorName = reply.AuthorName,
                AuthorRole = reply.AuthorRole,
                Message = reply.Message,
                IsInternal = reply.IsInternal,
                TemplateId = reply.TemplateId,
                TimeSpentMinutes = reply.TimeSpentMinutes,
                IsAiSuggested = reply.IsAiSuggested,
                CreatedAt = reply.CreatedAt,
                Attachments = reply.Attachments
                    .Where(attachment => visibleAttachments.Any(visible => visible.Id == attachment.Id))
                    .OrderBy(attachment => attachment.CreatedAt)
                    .ToList()
            });

        var timeline = visibleReplies
            .Select(reply => new TicketTimelineItemViewModel
            {
                OccurredAt = reply.CreatedAt,
                Reply = replyLookup[reply.Id]
            })
            .Concat(ticket.AuditEvents.Select(audit => new TicketTimelineItemViewModel
            {
                OccurredAt = audit.CreatedAt,
                AuditEvent = audit
            }))
            .OrderBy(item => item.OccurredAt)
            .ToList();

        var slaPolicy = dataStore.GetSlaPolicy(ticket.Priority);
        var slaDeadline = ticket.DueDate ?? slaPolicy?.Let(policy => ticket.CreatedAt.AddMinutes(policy.ResolutionTimeMinutes));
        var responseDeadline = slaPolicy?.Let(policy => ticket.CreatedAt.AddMinutes(policy.ResponseTimeMinutes));

        return new TicketDetailsViewModel
        {
            Ticket = ticket,
            ReplyForm = replyModel ?? new TicketReplyViewModel { Status = ticket.Status },
            EditForm = editModel ?? new TicketEditViewModel
            {
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Category = ticket.Category
            },
            TimeEntryForm = timeEntryModel ?? new TicketTimeEntryViewModel { Minutes = 15 },
            TimelineItems = timeline,
            VisibleAttachments = visibleAttachments.OrderByDescending(attachment => attachment.CreatedAt).ToList(),
            DescriptionHtml = SimpleMarkdown.Render(ticket.Description),
            TotalTrackedMinutes = ticket.TimeEntries.Sum(entry => entry.Minutes),
            CanManageTicket = isAgent,
            CanTrackTime = isAgent,
            CanAssignToSelf = isAgent && ticket.AssignedSupportId is null,
            IsCustomerView = currentUser.Role == UserRole.Customer,
            SlaSummary = FormatSlaSummary(ticket, slaDeadline),
            SlaClass = GetSlaBadgeClass(ticket, slaDeadline),
            ResponseSlaSummary = FormatResponseSlaSummary(ticket, responseDeadline),
            ActiveTimerStartedAtUtc = GetActiveTimerStartedAt(currentUser.Id, ticket.Id)
        };
    }

    private static bool CanSeeTicket(SupportTicket ticket, AppUser user)
    {
        return user.Role == UserRole.Support || user.Role == UserRole.Admin || ticket.CustomerId == user.Id;
    }

    private void PopulateCreateModel(CreateTicketViewModel model)
    {
        model.AvailableTemplates = dataStore.GetActiveTicketTemplates();
    }

    private void ApplyTemplateDefaults(CreateTicketViewModel model)
    {
        if (!model.SelectedTemplateId.HasValue)
        {
            return;
        }

        var template = dataStore.GetActiveTicketTemplates()
            .FirstOrDefault(item => item.Id == model.SelectedTemplateId.Value);
        if (template is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            model.Title = template.Title;
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            model.Description = template.Content;
        }

        if (model.Priority == TicketPriority.Medium)
        {
            model.Priority = template.Priority;
        }

        if (model.Category == TicketCategory.Other)
        {
            model.Category = template.Category;
        }
    }

    private string GetTimerCacheKey(int userId, int ticketId)
    {
        return $"{TimerCacheKeyPrefix}:{userId}:{ticketId}";
    }

    private DateTime? GetActiveTimerStartedAt(int userId, int ticketId)
    {
        return memoryCache.TryGetValue<DateTime>(GetTimerCacheKey(userId, ticketId), out var startedAt)
            ? startedAt
            : null;
    }

    private static string FormatSlaSummary(SupportTicket ticket, DateTime? deadline)
    {
        if (!deadline.HasValue)
        {
            return "SLA hedefi tanımlı değil";
        }

        if (ticket.Status is TicketStatus.Solved or TicketStatus.Closed)
        {
            var effectiveDate = ticket.ResolvedAt ?? ticket.LastUpdatedAt;
            return effectiveDate <= deadline.Value
                ? $"SLA içinde tamamlandı ({deadline.Value:dd.MM.yyyy HH:mm})"
                : $"SLA aşıldı, hedef {deadline.Value:dd.MM.yyyy HH:mm}";
        }

        var remaining = deadline.Value - DateTime.UtcNow;
        return remaining >= TimeSpan.Zero
            ? $"Kalan süre: {FormatDuration(remaining)}"
            : $"Gecikme: {FormatDuration(remaining.Duration())}";
    }

    private static string FormatResponseSlaSummary(SupportTicket ticket, DateTime? deadline)
    {
        if (!deadline.HasValue)
        {
            return "İlk yanıt SLA tanımlı değil";
        }

        if (ticket.FirstResponseAt.HasValue)
        {
            var firstResponseAt = ticket.FirstResponseAt.GetValueOrDefault();
            return firstResponseAt <= deadline.Value
                ? $"İlk yanıt zamanında verildi ({firstResponseAt:dd.MM.yyyy HH:mm})"
                : $"İlk yanıt SLA dışında verildi ({firstResponseAt:dd.MM.yyyy HH:mm})";
        }

        var remaining = deadline.Value - DateTime.UtcNow;
        return remaining >= TimeSpan.Zero
            ? $"İlk yanıt için {FormatDuration(remaining)} kaldı"
            : $"İlk yanıt SLA'sı {FormatDuration(remaining.Duration())} gecikti";
    }

    private static string GetSlaBadgeClass(SupportTicket ticket, DateTime? deadline)
    {
        if (!deadline.HasValue)
        {
            return "text-bg-secondary";
        }

        if (ticket.Status is TicketStatus.Solved or TicketStatus.Closed)
        {
            var effectiveDate = ticket.ResolvedAt ?? ticket.LastUpdatedAt;
            return effectiveDate <= deadline.Value ? "text-bg-success" : "text-bg-danger";
        }

        var remaining = deadline.Value - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
        {
            return "text-bg-danger";
        }

        return remaining <= TimeSpan.FromHours(4) ? "text-bg-warning" : "text-bg-success";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalHours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return totalHours > 0 ? $"{totalHours} sa {minutes} dk" : $"{Math.Max(1, minutes)} dk";
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

    private static string GetStatusBadgeClass(TicketStatus status) => status switch
    {
        TicketStatus.Open => "text-bg-warning",
        TicketStatus.Solved => "text-bg-success",
        TicketStatus.Closed => "text-bg-secondary",
        TicketStatus.InProgress => "text-bg-info",
        TicketStatus.WaitingCustomer => "text-bg-primary",
        TicketStatus.Cancelled => "text-bg-dark",
        _ => "text-bg-light"
    };
}

internal static class FunctionalExtensions
{
    public static TResult Let<TSource, TResult>(this TSource source, Func<TSource, TResult> selector)
    {
        return selector(source);
    }
}
