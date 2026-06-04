using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly IAppDataStore dataStore;

    public TicketsController(IAppDataStore dataStore)
    {
        this.dataStore = dataStore;
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
        var ticket = dataStore.FindTicket(id);
        if (currentUser is null || ticket is null)
        {
            return NotFound();
        }

        if (!CanSeeTicket(ticket, currentUser))
        {
            return Forbid();
        }

        ViewBag.ReplyModel = new TicketReplyViewModel { Status = ticket.Status };
        return View(ticket);
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    public IActionResult Create()
    {
        return View(new CreateTicketViewModel());
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateTicketViewModel model)
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ticket = dataStore.AddTicket(model, currentUser);
        TempData["Message"] = "Destek talebiniz oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
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
    public IActionResult Reply(int id, TicketReplyViewModel model)
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

        if (currentUser.Role == UserRole.Customer && ticket.Status == TicketStatus.Closed)
        {
            ModelState.AddModelError(string.Empty, "Kapanmış talebe cevap yazılamaz.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReplyModel = model;
            return View(nameof(Details), ticket);
        }

        if (currentUser.Role == UserRole.Customer)
        {
            model.Status = TicketStatus.Open;
        }

        dataStore.AddReply(id, model, currentUser);
        TempData["Message"] = "Cevabınız kaydedildi.";
        return RedirectToAction(nameof(Details), new { id });
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

    private static bool CanSeeTicket(SupportTicket ticket, AppUser user)
    {
        return user.Role == UserRole.Support || user.Role == UserRole.Admin || ticket.CustomerId == user.Id;
    }
}
