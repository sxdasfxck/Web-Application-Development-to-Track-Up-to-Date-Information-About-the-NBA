using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class CalendarController : Controller
    {
        private readonly InterfaceCalendarService _calendarService;

        public CalendarController(InterfaceCalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        public async Task<IActionResult> Calendar()
        {
            var calendar = await _calendarService.GetCalendarAsync();

            if (calendar == null)
            {
                return NotFound();
            }

            return View(calendar);
        }

        public async Task<IActionResult> Refresh()
        {
            if (_calendarService is CalendarService service)
            {
                service.ClearCache();
                var calendar = await _calendarService.GetCalendarAsync();
                if (calendar == null)
                {
                    return NotFound();
                }
                return View("Calendar", calendar);
            }

            return RedirectToAction("Calendar");
        }
    }
}