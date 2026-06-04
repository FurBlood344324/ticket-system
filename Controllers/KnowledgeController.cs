using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Controllers;

[Authorize]
public class KnowledgeController : Controller
{
    private readonly ApplicationDbContext dbContext;

    public KnowledgeController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IActionResult Index()
    {
        var categories = dbContext.KnowledgeCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Include(category => category.Articles.Where(article => article.IsPublished))
            .ToList();

        return View(categories);
    }

    public IActionResult Category(int id)
    {
        var category = dbContext.KnowledgeCategories
            .AsNoTracking()
            .Include(c => c.Articles.Where(article => article.IsPublished))
            .FirstOrDefault(c => c.Id == id && c.IsActive);

        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    public IActionResult Article(int id)
    {
        var article = dbContext.KnowledgeArticles
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == id && item.IsPublished);

        if (article is null)
        {
            return NotFound();
        }

        article.ViewCount += 1;
        dbContext.SaveChanges();

        return View(article);
    }
}
