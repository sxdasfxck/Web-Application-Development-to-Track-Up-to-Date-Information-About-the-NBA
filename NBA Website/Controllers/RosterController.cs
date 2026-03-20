using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class RosterController : Controller
    {
        private readonly InterfaceRosterService _rosterService;

        public RosterController(InterfaceRosterService rosterService)
        {
            _rosterService = rosterService;
        }

        public async Task<IActionResult> Roster(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev))
            {
                return RedirectToAction("TeamDetails", "TeamDetails");
            }

            var roster = await _rosterService.GetRosterInfoAsync(abbrev);

            if (roster == null)
            {
                return NotFound();
            }

            return View(roster);
        }
    }
}