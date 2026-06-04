using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize(Roles = nameof(Models.UserRole.Admin))]
public class AdminController : Controller
{
    private readonly IAppDataStore dataStore;

    public AdminController(IAppDataStore dataStore)
    {
        this.dataStore = dataStore;
    }

    public IActionResult Index()
    {
        var users = dataStore.GetUsers();
        return View(users);
    }
}
