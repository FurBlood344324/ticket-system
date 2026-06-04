using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationService notificationService;
    private readonly IAppDataStore dataStore;

    public NotificationsController(
        NotificationService notificationService,
        IAppDataStore dataStore)
    {
        this.notificationService = notificationService;
        this.dataStore = dataStore;
    }

    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Challenge();

        var notifications = await notificationService.GetNotificationsAsync(currentUser.Id, page, 20);
        var unreadCount = await notificationService.GetUnreadCountAsync(currentUser.Id);

        ViewBag.UnreadCount = unreadCount;
        ViewBag.CurrentPage = page;

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Challenge();

        await notificationService.MarkReadAsync(id, currentUser.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Challenge();

        await notificationService.MarkAllReadAsync(currentUser.Id);
        TempData["Message"] = "Tüm bildirimler okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Json(new { count = 0 });

        var count = await notificationService.GetUnreadCountAsync(currentUser.Id);
        return Json(new { count });
    }

    private AppUser? GetCurrentUser()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var id)) return null;
        return dataStore.GetUsers().FirstOrDefault(u => u.Id == id);
    }
}
