using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;
using NBA_Website.Models;

namespace NBA_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly InterfaceESPNService _espnService;

        public HomeController(InterfaceESPNService espnService)
        {
            _espnService = espnService;
        }

        public async Task<IActionResult> Index()
        {
            var news = await _espnService.GetNbaNewsAsync();
            return View(news);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}