using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;
using NBA_Website.Models;

namespace NBA_Website.Controllers
{
    public class TeamController : Controller
    {
        private readonly InterfaceTeamDetailService _teamDetailService;

        public TeamController(InterfaceTeamDetailService teamDetailService)
        {
            _teamDetailService = teamDetailService;
        }

        public async Task<IActionResult> TeamDetails(string abbrev)
        {

            if (string.IsNullOrEmpty(abbrev))
            {
                return RedirectToAction("Teams", "NBA");
            }

            var team = await _teamDetailService.GetTeamDetailAsync(abbrev);

            if (team == null)
            {
                return NotFound();
            }

            return View("~/Views/TeamDetails/TeamDetails.cshtml", team);
        }
    }
}