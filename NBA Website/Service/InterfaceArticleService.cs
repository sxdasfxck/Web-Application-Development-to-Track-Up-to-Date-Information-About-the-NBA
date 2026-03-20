using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceArticleService
    {
        Task<ArticleViewModel?> GetArticleByIdAsync(string articleId);
    }
}