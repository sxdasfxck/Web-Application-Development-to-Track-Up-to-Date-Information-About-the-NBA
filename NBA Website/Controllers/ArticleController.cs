using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;
using NBA_Website.Models;

namespace NBA_Website.Controllers
{
    public class ArticleController : Controller
    {
        private readonly InterfaceArticleService _articleService;

        public ArticleController(InterfaceArticleService articleService)
        {
            _articleService = articleService;
        }

        public async Task<IActionResult> Article(string id)
        {          
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }

            var article = await _articleService.GetArticleByIdAsync(id);
            
            if (article == null)
            {
                return NotFound();
            }
            return View(article);
        }
    }
}