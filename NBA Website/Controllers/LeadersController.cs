using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class LeadersController : Controller
    {
        private readonly InterfaceLeadersService _leadersService;

        public LeadersController(InterfaceLeadersService leadersService)
        {
            _leadersService = leadersService;
        }

        public async Task<IActionResult> Leaders()
        {
            var leaders = await _leadersService.GetLeadersAsync();
            return leaders == null ? NotFound() : View(leaders);
        }
    }
}