using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class MatchController : Controller
    {
        private readonly InterfaceMatchService _matchService;

        public MatchController(InterfaceMatchService matchService)
        {
            _matchService = matchService;
        }

        public async Task<IActionResult> Match(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Calendar", "Calendar");
            }

            var match = await _matchService.GetMatchAsync(id);

            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }
    }
}