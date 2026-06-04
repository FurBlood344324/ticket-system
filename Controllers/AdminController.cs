using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Helpers;
using TicketSupport.Models;
using TicketSupport.Services;

namespace TicketSupport.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly PasswordHasher passwordHasher;

    public AdminController(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private string CurrentUserName => User.Identity?.Name ?? "Yönetici";

    // ──────────────────────────────────────────────
    // GENEL BAKIŞ
    // ──────────────────────────────────────────────

    public IActionResult Index()
    {
        var model = new AdminOverviewViewModel
        {
            TotalUsers = dbContext.Users.Count(),
            ActiveUsers = dbContext.Users.Count(user => user.IsActive),
            TotalTickets = dbContext.Tickets.Count(ticket => !ticket.IsDeleted),
            OpenTickets = dbContext.Tickets.Count(ticket => !ticket.IsDeleted && ticket.Status == TicketStatus.Open),
            UnassignedTickets = dbContext.Tickets.Count(ticket => !ticket.IsDeleted && ticket.AssignedSupportId == null),
            DepartmentCount = dbContext.Departments.Count(),
            SlaPolicyCount = dbContext.SlaPolicies.Count(),
            TemplateCount = dbContext.TicketTemplates.Count(),
            CannedResponseCount = dbContext.CannedResponses.Count(),
            TagCount = dbContext.TicketTags.Count(),
            RecurringCount = dbContext.RecurringTickets.Count(),
            PublishedArticles = dbContext.KnowledgeArticles.Count(article => article.IsPublished),
            CategoryCount = dbContext.KnowledgeCategories.Count()
        };

        return View(model);
    }

    // ──────────────────────────────────────────────
    // KULLANICILAR
    // ──────────────────────────────────────────────

    public IActionResult Users()
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .OrderBy(user => user.FullName)
            .ToList();

        return View(users);
    }

    [HttpGet]
    public IActionResult UserCreate()
    {
        PopulateDepartments();
        return View("UserForm", new AdminUserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UserCreate(AdminUserCreateViewModel model)
    {
        if (dbContext.Users.Any(user => user.Email == model.Email.Trim()))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta zaten kayıtlı.");
        }

        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("UserForm", model);
        }

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            PasswordHash = passwordHasher.Hash(model.Password),
            Role = model.Role,
            DepartmentId = model.DepartmentId,
            JobTitle = Normalize(model.JobTitle),
            Phone = Normalize(model.Phone),
            IsActive = model.IsActive
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        TempData["Message"] = $"Kullanıcı oluşturuldu: {user.FullName}";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public IActionResult UserEdit(int id)
    {
        var user = dbContext.Users.Find(id);
        if (user is null)
        {
            return NotFound();
        }

        PopulateDepartments(user.DepartmentId);
        return View("UserEdit", new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            JobTitle = user.JobTitle,
            Phone = user.Phone,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UserEdit(AdminUserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("UserEdit", model);
        }

        var user = dbContext.Users.Find(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName.Trim();
        user.Role = model.Role;
        user.DepartmentId = model.DepartmentId;
        user.JobTitle = Normalize(model.JobTitle);
        user.Phone = Normalize(model.Phone);
        user.IsActive = model.IsActive;
        dbContext.SaveChanges();

        TempData["Message"] = $"Kullanıcı güncellendi: {user.FullName}";
        return RedirectToAction(nameof(Users));
    }

    // ──────────────────────────────────────────────
    // DEPARTMANLAR
    // ──────────────────────────────────────────────

    public IActionResult Departments()
    {
        return View(dbContext.Departments.AsNoTracking().OrderBy(d => d.Name).ToList());
    }

    [HttpGet]
    public IActionResult DepartmentCreate() => View("DepartmentForm", new DepartmentFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DepartmentCreate(DepartmentFormViewModel model)
    {
        if (dbContext.Departments.Any(d => d.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu isimde bir departman zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("DepartmentForm", model);
        }

        dbContext.Departments.Add(new Department
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim() ?? string.Empty,
            IsActive = model.IsActive
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Departman oluşturuldu.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpGet]
    public IActionResult DepartmentEdit(int id)
    {
        var department = dbContext.Departments.Find(id);
        if (department is null)
        {
            return NotFound();
        }

        return View("DepartmentForm", new DepartmentFormViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DepartmentEdit(DepartmentFormViewModel model)
    {
        if (dbContext.Departments.Any(d => d.Name == model.Name.Trim() && d.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu isimde bir departman zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("DepartmentForm", model);
        }

        var department = dbContext.Departments.Find(model.Id);
        if (department is null)
        {
            return NotFound();
        }

        department.Name = model.Name.Trim();
        department.Description = model.Description?.Trim() ?? string.Empty;
        department.IsActive = model.IsActive;
        dbContext.SaveChanges();

        TempData["Message"] = "Departman güncellendi.";
        return RedirectToAction(nameof(Departments));
    }

    // ──────────────────────────────────────────────
    // SLA POLİTİKALARI
    // ──────────────────────────────────────────────

    public IActionResult SlaPolicies()
    {
        return View(dbContext.SlaPolicies.AsNoTracking().OrderBy(p => p.Priority).ToList());
    }

    [HttpGet]
    public IActionResult SlaCreate() => View("SlaForm", new SlaPolicyFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SlaCreate(SlaPolicyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("SlaForm", model);
        }

        dbContext.SlaPolicies.Add(new SlaPolicy
        {
            Priority = model.Priority,
            ResponseTimeMinutes = model.ResponseTimeMinutes,
            ResolutionTimeMinutes = model.ResolutionTimeMinutes,
            BusinessHoursOnly = model.BusinessHoursOnly
        });
        dbContext.SaveChanges();

        TempData["Message"] = "SLA politikası oluşturuldu.";
        return RedirectToAction(nameof(SlaPolicies));
    }

    [HttpGet]
    public IActionResult SlaEdit(int id)
    {
        var policy = dbContext.SlaPolicies.Find(id);
        if (policy is null)
        {
            return NotFound();
        }

        return View("SlaForm", new SlaPolicyFormViewModel
        {
            Id = policy.Id,
            Priority = policy.Priority,
            ResponseTimeMinutes = policy.ResponseTimeMinutes,
            ResolutionTimeMinutes = policy.ResolutionTimeMinutes,
            BusinessHoursOnly = policy.BusinessHoursOnly
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SlaEdit(SlaPolicyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("SlaForm", model);
        }

        var policy = dbContext.SlaPolicies.Find(model.Id);
        if (policy is null)
        {
            return NotFound();
        }

        policy.Priority = model.Priority;
        policy.ResponseTimeMinutes = model.ResponseTimeMinutes;
        policy.ResolutionTimeMinutes = model.ResolutionTimeMinutes;
        policy.BusinessHoursOnly = model.BusinessHoursOnly;
        dbContext.SaveChanges();

        TempData["Message"] = "SLA politikası güncellendi.";
        return RedirectToAction(nameof(SlaPolicies));
    }

    // ──────────────────────────────────────────────
    // ŞABLONLAR
    // ──────────────────────────────────────────────

    public IActionResult Templates()
    {
        return View(dbContext.TicketTemplates.AsNoTracking().Include(t => t.Department).OrderBy(t => t.Title).ToList());
    }

    [HttpGet]
    public IActionResult TemplateCreate()
    {
        PopulateDepartments();
        return View("TemplateForm", new TicketTemplateFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TemplateCreate(TicketTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("TemplateForm", model);
        }

        dbContext.TicketTemplates.Add(new TicketTemplate
        {
            Title = model.Title.Trim(),
            Description = Normalize(model.Description),
            Category = model.Category,
            Priority = model.Priority,
            Content = model.Content.Trim(),
            DepartmentId = model.DepartmentId,
            IsActive = model.IsActive,
            CreatedById = CurrentUserId,
            CreatedByName = CurrentUserName
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Şablon oluşturuldu.";
        return RedirectToAction(nameof(Templates));
    }

    [HttpGet]
    public IActionResult TemplateEdit(int id)
    {
        var template = dbContext.TicketTemplates.Find(id);
        if (template is null)
        {
            return NotFound();
        }

        PopulateDepartments(template.DepartmentId);
        return View("TemplateForm", new TicketTemplateFormViewModel
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            Category = template.Category,
            Priority = template.Priority,
            Content = template.Content,
            DepartmentId = template.DepartmentId,
            IsActive = template.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TemplateEdit(TicketTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("TemplateForm", model);
        }

        var template = dbContext.TicketTemplates.Find(model.Id);
        if (template is null)
        {
            return NotFound();
        }

        template.Title = model.Title.Trim();
        template.Description = Normalize(model.Description);
        template.Category = model.Category;
        template.Priority = model.Priority;
        template.Content = model.Content.Trim();
        template.DepartmentId = model.DepartmentId;
        template.IsActive = model.IsActive;
        dbContext.SaveChanges();

        TempData["Message"] = "Şablon güncellendi.";
        return RedirectToAction(nameof(Templates));
    }

    // ──────────────────────────────────────────────
    // HAZIR YANITLAR
    // ──────────────────────────────────────────────

    public IActionResult CannedResponses()
    {
        return View(dbContext.CannedResponses.AsNoTracking().OrderBy(r => r.Title).ToList());
    }

    [HttpGet]
    public IActionResult CannedCreate() => View("CannedForm", new CannedResponseFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CannedCreate(CannedResponseFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CannedForm", model);
        }

        dbContext.CannedResponses.Add(new CannedResponse
        {
            Title = model.Title.Trim(),
            Category = model.Category.Trim(),
            Content = model.Content.Trim(),
            IsShared = model.IsShared,
            CreatedById = CurrentUserId,
            CreatedByName = CurrentUserName
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Hazır yanıt oluşturuldu.";
        return RedirectToAction(nameof(CannedResponses));
    }

    [HttpGet]
    public IActionResult CannedEdit(int id)
    {
        var response = dbContext.CannedResponses.Find(id);
        if (response is null)
        {
            return NotFound();
        }

        return View("CannedForm", new CannedResponseFormViewModel
        {
            Id = response.Id,
            Title = response.Title,
            Category = response.Category,
            Content = response.Content,
            IsShared = response.IsShared
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CannedEdit(CannedResponseFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CannedForm", model);
        }

        var response = dbContext.CannedResponses.Find(model.Id);
        if (response is null)
        {
            return NotFound();
        }

        response.Title = model.Title.Trim();
        response.Category = model.Category.Trim();
        response.Content = model.Content.Trim();
        response.IsShared = model.IsShared;
        dbContext.SaveChanges();

        TempData["Message"] = "Hazır yanıt güncellendi.";
        return RedirectToAction(nameof(CannedResponses));
    }

    // ──────────────────────────────────────────────
    // ETİKETLER
    // ──────────────────────────────────────────────

    public IActionResult Tags()
    {
        return View(dbContext.TicketTags.AsNoTracking().OrderBy(t => t.Name).ToList());
    }

    [HttpGet]
    public IActionResult TagCreate() => View("TagForm", new TagFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TagCreate(TagFormViewModel model)
    {
        if (dbContext.TicketTags.Any(t => t.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu etiket zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("TagForm", model);
        }

        dbContext.TicketTags.Add(new TicketTag
        {
            Name = model.Name.Trim(),
            Color = model.Color.Trim()
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Etiket oluşturuldu.";
        return RedirectToAction(nameof(Tags));
    }

    [HttpGet]
    public IActionResult TagEdit(int id)
    {
        var tag = dbContext.TicketTags.Find(id);
        if (tag is null)
        {
            return NotFound();
        }

        return View("TagForm", new TagFormViewModel { Id = tag.Id, Name = tag.Name, Color = tag.Color });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TagEdit(TagFormViewModel model)
    {
        if (dbContext.TicketTags.Any(t => t.Name == model.Name.Trim() && t.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu etiket zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("TagForm", model);
        }

        var tag = dbContext.TicketTags.Find(model.Id);
        if (tag is null)
        {
            return NotFound();
        }

        tag.Name = model.Name.Trim();
        tag.Color = model.Color.Trim();
        dbContext.SaveChanges();

        TempData["Message"] = "Etiket güncellendi.";
        return RedirectToAction(nameof(Tags));
    }

    // ──────────────────────────────────────────────
    // YİNELENEN TALEPLER
    // ──────────────────────────────────────────────

    public IActionResult Recurring()
    {
        return View(dbContext.RecurringTickets.AsNoTracking().Include(r => r.Department).OrderBy(r => r.Title).ToList());
    }

    [HttpGet]
    public IActionResult RecurringCreate()
    {
        PopulateDepartments();
        return View("RecurringForm", new RecurringTicketFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RecurringCreate(RecurringTicketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("RecurringForm", model);
        }

        dbContext.RecurringTickets.Add(new RecurringTicket
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Category = model.Category,
            Priority = model.Priority,
            DepartmentId = model.DepartmentId,
            CronExpression = model.CronExpression.Trim(),
            IsActive = model.IsActive,
            NextRunAt = ToUtc(model.NextRunAtLocal),
            CreatedById = CurrentUserId
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Yinelenen talep oluşturuldu.";
        return RedirectToAction(nameof(Recurring));
    }

    [HttpGet]
    public IActionResult RecurringEdit(int id)
    {
        var recurring = dbContext.RecurringTickets.Find(id);
        if (recurring is null)
        {
            return NotFound();
        }

        PopulateDepartments(recurring.DepartmentId);
        return View("RecurringForm", new RecurringTicketFormViewModel
        {
            Id = recurring.Id,
            Title = recurring.Title,
            Description = recurring.Description,
            Category = recurring.Category,
            Priority = recurring.Priority,
            DepartmentId = recurring.DepartmentId,
            CronExpression = recurring.CronExpression,
            IsActive = recurring.IsActive,
            NextRunAtLocal = recurring.NextRunAt?.ToLocal()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RecurringEdit(RecurringTicketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateDepartments(model.DepartmentId);
            return View("RecurringForm", model);
        }

        var recurring = dbContext.RecurringTickets.Find(model.Id);
        if (recurring is null)
        {
            return NotFound();
        }

        recurring.Title = model.Title.Trim();
        recurring.Description = model.Description.Trim();
        recurring.Category = model.Category;
        recurring.Priority = model.Priority;
        recurring.DepartmentId = model.DepartmentId;
        recurring.CronExpression = model.CronExpression.Trim();
        recurring.IsActive = model.IsActive;
        recurring.NextRunAt = ToUtc(model.NextRunAtLocal);
        dbContext.SaveChanges();

        TempData["Message"] = "Yinelenen talep güncellendi.";
        return RedirectToAction(nameof(Recurring));
    }

    // ──────────────────────────────────────────────
    // BİLGİ BANKASI — KATEGORİLER
    // ──────────────────────────────────────────────

    public IActionResult Categories()
    {
        return View(dbContext.KnowledgeCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList());
    }

    [HttpGet]
    public IActionResult CategoryCreate() => View("CategoryForm", new KnowledgeCategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CategoryCreate(KnowledgeCategoryFormViewModel model)
    {
        if (dbContext.KnowledgeCategories.Any(c => c.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu isimde bir kategori zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        dbContext.KnowledgeCategories.Add(new KnowledgeCategory
        {
            Name = model.Name.Trim(),
            Description = Normalize(model.Description),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Kategori oluşturuldu.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public IActionResult CategoryEdit(int id)
    {
        var category = dbContext.KnowledgeCategories.Find(id);
        if (category is null)
        {
            return NotFound();
        }

        return View("CategoryForm", new KnowledgeCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CategoryEdit(KnowledgeCategoryFormViewModel model)
    {
        if (dbContext.KnowledgeCategories.Any(c => c.Name == model.Name.Trim() && c.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu isimde bir kategori zaten var.");
        }

        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var category = dbContext.KnowledgeCategories.Find(model.Id);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = model.Name.Trim();
        category.Description = Normalize(model.Description);
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        dbContext.SaveChanges();

        TempData["Message"] = "Kategori güncellendi.";
        return RedirectToAction(nameof(Categories));
    }

    // ──────────────────────────────────────────────
    // BİLGİ BANKASI — MAKALELER
    // ──────────────────────────────────────────────

    public IActionResult Articles()
    {
        return View(dbContext.KnowledgeArticles.AsNoTracking().Include(a => a.Category).OrderByDescending(a => a.UpdatedAt).ToList());
    }

    [HttpGet]
    public IActionResult ArticleCreate()
    {
        PopulateCategories();
        return View("ArticleForm", new KnowledgeArticleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ArticleCreate(KnowledgeArticleFormViewModel model)
    {
        var slug = BuildSlug(model.Slug, model.Title);
        if (dbContext.KnowledgeArticles.Any(a => a.Slug == slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu slug zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            PopulateCategories(model.KnowledgeCategoryId);
            return View("ArticleForm", model);
        }

        dbContext.KnowledgeArticles.Add(new KnowledgeArticle
        {
            Title = model.Title.Trim(),
            Slug = slug,
            Content = model.Content.Trim(),
            KnowledgeCategoryId = model.KnowledgeCategoryId,
            AuthorId = CurrentUserId,
            AuthorName = CurrentUserName,
            IsPublished = model.IsPublished,
            PublishedAt = model.IsPublished ? DateTime.UtcNow : null,
            MetaKeywords = Normalize(model.MetaKeywords),
            MetaDescription = Normalize(model.MetaDescription)
        });
        dbContext.SaveChanges();

        TempData["Message"] = "Makale oluşturuldu.";
        return RedirectToAction(nameof(Articles));
    }

    [HttpGet]
    public IActionResult ArticleEdit(int id)
    {
        var article = dbContext.KnowledgeArticles.Find(id);
        if (article is null)
        {
            return NotFound();
        }

        PopulateCategories(article.KnowledgeCategoryId);
        return View("ArticleForm", new KnowledgeArticleFormViewModel
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Content = article.Content,
            KnowledgeCategoryId = article.KnowledgeCategoryId,
            MetaKeywords = article.MetaKeywords,
            MetaDescription = article.MetaDescription,
            IsPublished = article.IsPublished
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ArticleEdit(KnowledgeArticleFormViewModel model)
    {
        var slug = BuildSlug(model.Slug, model.Title);
        if (dbContext.KnowledgeArticles.Any(a => a.Slug == slug && a.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu slug zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            PopulateCategories(model.KnowledgeCategoryId);
            return View("ArticleForm", model);
        }

        var article = dbContext.KnowledgeArticles.Find(model.Id);
        if (article is null)
        {
            return NotFound();
        }

        var wasPublished = article.IsPublished;
        article.Title = model.Title.Trim();
        article.Slug = slug;
        article.Content = model.Content.Trim();
        article.KnowledgeCategoryId = model.KnowledgeCategoryId;
        article.MetaKeywords = Normalize(model.MetaKeywords);
        article.MetaDescription = Normalize(model.MetaDescription);
        article.IsPublished = model.IsPublished;
        if (model.IsPublished && !wasPublished)
        {
            article.PublishedAt = DateTime.UtcNow;
        }
        else if (!model.IsPublished)
        {
            article.PublishedAt = null;
        }

        dbContext.SaveChanges();

        TempData["Message"] = "Makale güncellendi.";
        return RedirectToAction(nameof(Articles));
    }

    // ──────────────────────────────────────────────
    // YARDIMCILAR
    // ──────────────────────────────────────────────

    private void PopulateDepartments(int? selected = null)
    {
        ViewBag.Departments = new SelectList(
            dbContext.Departments.AsNoTracking().OrderBy(d => d.Name).ToList(),
            nameof(Department.Id),
            nameof(Department.Name),
            selected);
    }

    private void PopulateCategories(int? selected = null)
    {
        ViewBag.Categories = new SelectList(
            dbContext.KnowledgeCategories.AsNoTracking().OrderBy(c => c.Name).ToList(),
            nameof(KnowledgeCategory.Id),
            nameof(KnowledgeCategory.Name),
            selected);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? ToUtc(DateTime? local)
    {
        if (!local.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(local.Value, DateTimeKind.Local).ToUniversalTime();
    }

    private static string BuildSlug(string? explicitSlug, string title)
    {
        var source = string.IsNullOrWhiteSpace(explicitSlug) ? title : explicitSlug;
        source = source.Trim().ToLowerInvariant();

        var map = new Dictionary<char, char>
        {
            ['ı'] = 'i', ['ğ'] = 'g', ['ü'] = 'u', ['ş'] = 's', ['ö'] = 'o', ['ç'] = 'c'
        };

        var builder = new StringBuilder();
        var previousDash = false;
        foreach (var ch in source)
        {
            var c = map.TryGetValue(ch, out var mapped) ? mapped : ch;
            if (char.IsLetterOrDigit(c) && c < 128)
            {
                builder.Append(c);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }
}
