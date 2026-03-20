using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class DepthChartController : Controller
    {
        private readonly InterfaceDepthChartService _depthChartService;

        public DepthChartController(InterfaceDepthChartService depthChartService)
        {
            _depthChartService = depthChartService;
        }

        public async Task<IActionResult> DepthChart(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev))
            {
                return RedirectToAction("TeamDetails", "Team");
            }

            var depthChart = await _depthChartService.GetTeamDepthChartAsync(abbrev);

            if (depthChart == null)
            {
                return NotFound();
            }

            return View(depthChart);
        }
    }
}