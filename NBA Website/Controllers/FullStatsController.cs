using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    [Route("FullStats")]
    public class FullStatsController : Controller
    {
        private readonly InterfaceFullStatsService _statsService;

        public FullStatsController(InterfaceFullStatsService statsService)
        {
            _statsService = statsService;
        }

        [HttpGet]
        public async Task<IActionResult> FullStats(string sortBy = "PTS", string type = "main", int page = 1)
        {
            var stats = await _statsService.GetFullStatsAsync(sortBy, type, page);
            return stats == null ? NotFound() : View(stats);
        }
    }
}