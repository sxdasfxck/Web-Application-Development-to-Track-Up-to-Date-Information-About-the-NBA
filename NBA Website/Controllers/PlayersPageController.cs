using Microsoft.AspNetCore.Mvc;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    [Route("Players")]
    public class PlayersPageController : Controller
    {
        private readonly InterfacePlayersPageService _playersService;

        public PlayersPageController(InterfacePlayersPageService playersService)
        {
            _playersService = playersService;
        }

        [HttpGet("")]
        public async Task<IActionResult> PlayersPage(int page = 1)
        {
            var players = await _playersService.GetPlayersListAsync(page);
            return players == null ? NotFound() : View(players);
        }

        [HttpGet("{playerId}")]
        public async Task<IActionResult> PlayersPage(string playerId)
        {
            var player = await _playersService.GetPlayerDetailsAsync(playerId);
            return player == null ? NotFound() : View(player);
        }
    }
}