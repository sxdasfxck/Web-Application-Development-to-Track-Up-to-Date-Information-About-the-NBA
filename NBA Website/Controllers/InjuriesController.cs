using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class InjuriesController : Controller
    {
        private readonly InterfaceInjuriesService _injuriesService;

        public InjuriesController(InterfaceInjuriesService injuriesService)
        {
            _injuriesService = injuriesService;
        }

        public async Task<IActionResult> Injuries(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev))
            {
                return RedirectToAction("TeamDetails", "TeamDetails");
            }

            var injuries = await _injuriesService.GetTeamInjuriesAsync(abbrev);

            if (injuries == null)
            {
                return NotFound();
            }

            return View(injuries);
        }
    }
}