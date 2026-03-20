using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private static List<PlayerSearchItem> _playersCache = new List<PlayerSearchItem>();
        private static DateTime _cacheTime = DateTime.MinValue;
        private static bool _isLoading = false;

        public SearchController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        [HttpGet("players")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new List<object>());

            try
            {
                if (!_playersCache.Any())
                    return Ok(new List<object>());

                var results = _playersCache
                    .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToList();

                return Ok(results);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("load")]
        public IActionResult StartLoading()
        {
            if (!_isLoading && !_playersCache.Any())
            {
                _ = Task.Run(() => LoadAllPlayersFastAsync());
                return Ok(new { loading = true });
            }
            return Ok(new { loading = _isLoading, count = _playersCache.Count });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                count = _playersCache.Count,
                loading = _isLoading,
                lastUpdate = _cacheTime
            });
        }

        private async Task LoadAllPlayersFastAsync()
        {
            if (_isLoading) return;

            _isLoading = true;
            _playersCache.Clear();

            try
            {
                var listUrl = "https://sports.core.api.espn.com/v2/sports/basketball/leagues/nba/athletes?limit=1000";
                var listResponse = await _httpClient.GetStringAsync(listUrl);
                var listDoc = JsonDocument.Parse(listResponse);
                var items = listDoc.RootElement.GetProperty("items").EnumerateArray();

                var playerRefs = items
                    .Select(item => item.GetProperty("$ref").GetString())
                    .Where(refUrl => refUrl != null)
                    .ToList();

                var batchSize = 200;
                for (int i = 0; i < playerRefs.Count; i += batchSize)
                {
                    var batch = playerRefs.Skip(i).Take(batchSize).ToList();
                    var tasks = batch.Select(refUrl => LoadPlayerAsync(refUrl));
                    var batchResults = await Task.WhenAll(tasks);

                    lock (_playersCache)
                    {
                        _playersCache.AddRange(batchResults.Where(p => p != null));
                    }

                    if (i + batchSize < playerRefs.Count)
                        await Task.Delay(100);
                }

                _cacheTime = DateTime.Now;
            }
            catch
            {
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task<PlayerSearchItem?> LoadPlayerAsync(string refUrl)
        {
            try
            {
                var playerId = ExtractPlayerIdFromRef(refUrl);
                var playerResponse = await _httpClient.GetStringAsync(refUrl);
                using var playerDoc = JsonDocument.Parse(playerResponse);
                var root = playerDoc.RootElement;

                var displayName = GetStringValue(root, "displayName") ??
                                  GetStringValue(root, "fullName") ??
                                  GetStringValue(root, "shortName");

                if (string.IsNullOrEmpty(displayName)) return null;

                var teamAbbrev = ExtractTeamAbbrevFast(root);
                if (string.IsNullOrEmpty(teamAbbrev)) return null;

                var position = GetNestedString(root, "position", "abbreviation");
                var headshotUrl = GetNestedString(root, "headshot", "href");

                return new PlayerSearchItem
                {
                    Id = playerId,
                    Name = displayName,
                    TeamAbbrev = teamAbbrev,
                    Position = position,
                    HeadshotUrl = headshotUrl ?? $"https://a.espncdn.com/i/headshots/nba/players/full/{playerId}.png"
                };
            }
            catch
            {
                return null;
            }
        }

        private string? GetStringValue(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }

        private string? GetNestedString(JsonElement element, string outer, string inner)
        {
            if (element.TryGetProperty(outer, out var outerObj) &&
                outerObj.TryGetProperty(inner, out var innerProp) &&
                innerProp.ValueKind == JsonValueKind.String)
            {
                return innerProp.GetString();
            }
            return null;
        }

        private string ExtractPlayerIdFromRef(string refUrl)
        {
            var match = Regex.Match(refUrl, @"athletes/(\d+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        private string ExtractTeamAbbrevFast(JsonElement playerRoot)
        {
            try
            {
                if (playerRoot.TryGetProperty("team", out var teamElement))
                {
                    if (teamElement.TryGetProperty("$ref", out var teamRef))
                    {
                        var teamRefString = teamRef.GetString();
                        var match = Regex.Match(teamRefString, @"teams/(\d+)");
                        if (match.Success)
                            return GetTeamAbbrevFromId(match.Groups[1].Value);
                    }

                    if (teamElement.TryGetProperty("abbreviation", out var abbrev))
                    {
                        var abbrevValue = abbrev.GetString();
                        if (!string.IsNullOrEmpty(abbrevValue))
                            return abbrevValue;
                    }

                    if (teamElement.TryGetProperty("displayName", out var teamName))
                    {
                        var teamNameValue = teamName.GetString();
                        return GetTeamAbbrevFromName(teamNameValue);
                    }
                }

                if (playerRoot.TryGetProperty("teamAbbreviation", out var directAbbrev))
                    return directAbbrev.GetString() ?? "";
            }
            catch { }
            return "";
        }

        private string GetTeamAbbrevFromId(string teamId)
        {
            return teamId switch
            {
                "1" => "ATL",
                "2" => "BOS",
                "3" => "NO",
                "4" => "CHI",
                "5" => "CLE",
                "6" => "DAL",
                "7" => "DEN",
                "8" => "DET",
                "9" => "GS",
                "10" => "HOU",
                "11" => "IND",
                "12" => "LAC",
                "13" => "LAL",
                "14" => "MIA",
                "15" => "MIL",
                "16" => "MIN",
                "17" => "BKN",
                "18" => "NY",
                "19" => "ORL",
                "20" => "PHI",
                "21" => "PHX",
                "22" => "POR",
                "23" => "SAC",
                "24" => "SA",
                "25" => "OKC",
                "26" => "UTAH",
                "27" => "WSH",
                "28" => "TOR",
                "29" => "MEM",
                "30" => "CHA",
                _ => ""
            };
        }

        private string GetTeamAbbrevFromName(string? teamName)
        {
            if (string.IsNullOrEmpty(teamName)) return "";

            var teamNameToAbbrev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Atlanta Hawks", "ATL"}, {"Boston Celtics", "BOS"}, {"Brooklyn Nets", "BKN"},
                {"Charlotte Hornets", "CHA"}, {"Chicago Bulls", "CHI"}, {"Cleveland Cavaliers", "CLE"},
                {"Dallas Mavericks", "DAL"}, {"Denver Nuggets", "DEN"}, {"Detroit Pistons", "DET"},
                {"Golden State Warriors", "GS"}, {"Houston Rockets", "HOU"}, {"Indiana Pacers", "IND"},
                {"LA Clippers", "LAC"}, {"Los Angeles Lakers", "LAL"}, {"Memphis Grizzlies", "MEM"},
                {"Miami Heat", "MIA"}, {"Milwaukee Bucks", "MIL"}, {"Minnesota Timberwolves", "MIN"},
                {"New Orleans Pelicans", "NO"}, {"New York Knicks", "NY"}, {"Oklahoma City Thunder", "OKC"},
                {"Orlando Magic", "ORL"}, {"Philadelphia 76ers", "PHI"}, {"Phoenix Suns", "PHX"},
                {"Portland Trail Blazers", "POR"}, {"Sacramento Kings", "SAC"}, {"San Antonio Spurs", "SA"},
                {"Toronto Raptors", "TOR"}, {"Utah Jazz", "UTAH"}, {"Washington Wizards", "WSH"}
            };

            return teamNameToAbbrev.ContainsKey(teamName) ? teamNameToAbbrev[teamName] : "";
        }
    }

    public class PlayerSearchItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TeamAbbrev { get; set; }
        public string? Position { get; set; }
        public string? HeadshotUrl { get; set; }
    }
}