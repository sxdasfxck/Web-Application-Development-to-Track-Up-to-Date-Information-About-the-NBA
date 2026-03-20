using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly InterfaceScheduleService _scheduleService;

        public ScheduleController(InterfaceScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        public async Task<IActionResult> Schedule(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev))
            {
                return RedirectToAction("TeamDetails", "Team");
            }

            var schedule = await _scheduleService.GetTeamScheduleAsync(abbrev);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }
    }
}