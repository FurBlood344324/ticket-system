namespace TicketSupport.Models;

public class RelatedArticle
{
    public int ArticleId { get; set; }
    public KnowledgeArticle? Article { get; set; }
    public int RelatedArticleId { get; set; }
    public KnowledgeArticle? Related { get; set; }
}
