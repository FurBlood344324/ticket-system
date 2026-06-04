using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAppDataStore dataStore;

    public DashboardController(IAppDataStore dataStore)
    {
        this.dataStore = dataStore;
    }

    public IActionResult Index()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null) return Challenge();
        var model = dataStore.GetDashboardData(currentUser);
        return View(model);
    }

    private Models.AppUser? GetCurrentUser()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var id)) return null;
        return dataStore.GetUsers().FirstOrDefault(user => user.Id == id);
    }
}
