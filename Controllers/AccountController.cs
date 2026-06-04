using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSupport.Data;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

public class AccountController : Controller
{
    private readonly IAppDataStore dataStore;
    private readonly ApplicationDbContext dbContext;
    private readonly PasswordHasher passwordHasher;

    public AccountController(IAppDataStore dataStore, ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        this.dataStore = dataStore;
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = dataStore.FindUserByEmail(model.Email);
        if (user is null || !dataStore.IsPasswordValid(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        await SignIn(user);
        return RedirectToAction("Index", "Dashboard");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (dataStore.FindUserByEmail(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta zaten kayıtlı.");
            return View(model);
        }

        var user = dataStore.AddCustomer(model);
        await SignIn(user);
        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var user = dbContext.Users.FirstOrDefault(item => item.Id == userId && item.IsActive);
        if (user is null)
        {
            return Challenge();
        }

        if (!passwordHasher.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mevcut şifre hatalı.");
            return View(model);
        }

        user.PasswordHash = passwordHasher.Hash(model.NewPassword);
        dbContext.SaveChanges();

        TempData["Message"] = "Şifreniz başarıyla değiştirildi.";
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task SignIn(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
