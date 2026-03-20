using Microsoft.AspNetCore.Mvc;
using NBA_Website.Models;
using NBA_Website.Services;

namespace NBA_Website.Controllers
{
    public class NBAController : Controller
    {
        private readonly InterfaceESPNService _espnService;
        private readonly InterfaceStandingsService _standingsService;

        public NBAController(InterfaceESPNService espnService, InterfaceStandingsService standingsService)
        {
            _espnService = espnService;
            _standingsService = standingsService;
        }

        public async Task<IActionResult> Teams()
        {
            var teams = await _espnService.GetNbaTeamsAsync();
            return View(teams);
        }

        public async Task<IActionResult> Standings(string group = "conference")
        {
            var standings = await _standingsService.GetStandingsAsync();

            ViewBag.CurrentGroup = group;

            var allStandings = standings
                .OrderByDescending(s => s.Wins)
                .ToList();
            ViewBag.AllStandings = allStandings;

            var eastern = standings.Where(s => s.Conference == "Eastern Conference")
                .OrderByDescending(s => s.Wins)
                .ToList();

            var western = standings.Where(s => s.Conference == "Western Conference")
                .OrderByDescending(s => s.Wins)
                .ToList();

            ViewBag.EasternStandings = eastern;
            ViewBag.WesternStandings = western;

            var divisionGroups = new Dictionary<string, List<StandingViewModel>>();

            var teamDivisions = new Dictionary<string, string>
            {
                // Atlantic
                { "Boston Celtics", "Atlantic" },
                { "New York Knicks", "Atlantic" },
                { "Toronto Raptors", "Atlantic" },
                { "Philadelphia 76ers", "Atlantic" },
                { "Brooklyn Nets", "Atlantic" },
                
                // Central
                { "Detroit Pistons", "Central" },
                { "Cleveland Cavaliers", "Central" },
                { "Milwaukee Bucks", "Central" },
                { "Chicago Bulls", "Central" },
                { "Indiana Pacers", "Central" },
                
                // Southeast
                { "Orlando Magic", "Southeast" },
                { "Miami Heat", "Southeast" },
                { "Atlanta Hawks", "Southeast" },
                { "Charlotte Hornets", "Southeast" },
                { "Washington Wizards", "Southeast" },
                
                // Northwest
                { "Oklahoma City Thunder", "Northwest" },
                { "Minnesota Timberwolves", "Northwest" },
                { "Denver Nuggets", "Northwest" },
                { "Portland Trail Blazers", "Northwest" },
                { "Utah Jazz", "Northwest" },
                
                // Pacific
                { "Los Angeles Lakers", "Pacific" },
                { "Phoenix Suns", "Pacific" },
                { "Golden State Warriors", "Pacific" },
                { "LA Clippers", "Pacific" },
                { "Sacramento Kings", "Pacific" },
                
                // Southwest
                { "San Antonio Spurs", "Southwest" },
                { "Houston Rockets", "Southwest" },
                { "Memphis Grizzlies", "Southwest" },
                { "Dallas Mavericks", "Southwest" },
                { "New Orleans Pelicans", "Southwest" }
            };

            foreach (var team in standings)
            {
                if (teamDivisions.TryGetValue(team.TeamName, out string division))
                {
                    if (!divisionGroups.ContainsKey(division))
                        divisionGroups[division] = new List<StandingViewModel>();

                    divisionGroups[division].Add(team);
                }
            }

            foreach (var div in divisionGroups.Keys.ToList())
            {
                divisionGroups[div] = divisionGroups[div]
                    .OrderByDescending(t => t.Wins)
                    .ToList();
            }

            ViewBag.DivisionGroups = divisionGroups;

            return View();
        }
    }
}