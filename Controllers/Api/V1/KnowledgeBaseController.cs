using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Controllers.Api.V1;

/// <summary>
/// Knowledge base endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/knowledge-base")]
[EnableRateLimiting("api")]
public class KnowledgeBaseController : ApiControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public KnowledgeBaseController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Lists published knowledge base articles.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<KnowledgeArticleSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<KnowledgeArticleSummaryDto>>>> GetArticles(CancellationToken cancellationToken)
    {
        var articles = await dbContext.KnowledgeArticles
            .AsNoTracking()
            .Include(article => article.Category)
            .Where(article => article.IsPublished)
            .OrderByDescending(article => article.PublishedAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<KnowledgeArticleSummaryDto>>.Ok(articles.Select(article => article.ToSummaryDto()).ToList()));
    }

    /// <summary>
    /// Gets a published knowledge base article.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<KnowledgeArticleDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDetailDto>>> GetArticle(int id, CancellationToken cancellationToken)
    {
        var article = await dbContext.KnowledgeArticles
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsPublished, cancellationToken);

        if (article is null)
        {
            return NotFound(ApiResponse<KnowledgeArticleDetailDto>.Fail("Article not found."));
        }

        article.ViewCount += 1;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<KnowledgeArticleDetailDto>.Ok(article.ToDetailDto()));
    }
}
