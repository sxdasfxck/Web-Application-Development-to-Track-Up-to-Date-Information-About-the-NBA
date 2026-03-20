using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Services
{
    public class LeadersService : InterfaceLeadersService
    {
        private readonly HttpClient _httpClient;

        public LeadersService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<LeadersViewModel?> GetLeadersAsync()
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://www.espn.com/nba/stats");
                var json = ExtractJsonFromHtml(html);

                return string.IsNullOrEmpty(json) ? null : ParseLeadersFromJson(json);
            }
            catch
            {
                return null;
            }
        }

        private string ExtractJsonFromHtml(string html)
        {
            var pattern = @"window\['__espnfitt__'\]\s*=\s*({.*?});\s*</script>";
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : null;
        }

        private LeadersViewModel? ParseLeadersFromJson(string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                var result = new LeadersViewModel();

                if (root.TryGetProperty("page", out var page) &&
                    page.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("statistics", out var statistics) &&
                    statistics.TryGetProperty("leaders", out var leaders))
                {
                    foreach (var category in leaders.EnumerateObject())
                    {
                        var categoryData = category.Value;

                        if (categoryData.TryGetProperty("groups", out var groups))
                        {
                            foreach (var group in groups.EnumerateArray())
                            {
                                if (group.TryGetProperty("leaders", out var groupLeaders))
                                {
                                    var parsedPlayers = ParseLeaders(groupLeaders);
                                    var header = group.GetProperty("header").GetString();

                                    switch (header)
                                    {
                                        case "Points":
                                            result.PointsLeaders = parsedPlayers;
                                            break;
                                        case "Assists":
                                            result.AssistsLeaders = parsedPlayers;
                                            break;
                                        case "3-Pointers Made":
                                            result.ThreePointLeaders = parsedPlayers;
                                            break;
                                        case "Rebounds":
                                            result.ReboundsLeaders = parsedPlayers;
                                            break;
                                        case "Blocks":
                                            result.BlocksLeaders = parsedPlayers;
                                            break;
                                        case "Steals":
                                            result.StealsLeaders = parsedPlayers;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                return result.PointsLeaders.Count > 0 ? result : null;
            }
        }

        private List<PlayerLeaders> ParseLeaders(JsonElement leaders)
        {
            var players = new List<PlayerLeaders>();

            foreach (var leader in leaders.EnumerateArray())
            {
                if (players.Count >= 5) break;

                var player = new PlayerLeaders
                {
                    Rank = leader.GetProperty("rank").GetInt32().ToString(),
                    PlayerName = leader.GetProperty("name").GetString() ?? leader.GetProperty("shortName").GetString() ?? "",
                    Value = leader.GetProperty("statValue").GetString() ?? "-"
                };

                if (leader.TryGetProperty("team", out var team))
                {
                    string teamValue = team.GetString() ?? "";

                    if (teamValue.Contains("/"))
                    {
                        var teams = teamValue.Split('/');
                        player.TeamAbbrev = teams[teams.Length - 1].Trim();
                    }
                    else
                    {
                        player.TeamAbbrev = teamValue;
                    }
                }

                if (leader.TryGetProperty("headshot", out var headshot) &&
                    headshot.TryGetProperty("href", out var href))
                {
                    player.HeadshotUrl = href.GetString() ?? "";

                    var hrefString = player.HeadshotUrl;
                    var match = System.Text.RegularExpressions.Regex.Match(hrefString, @"(\d+)\.png");
                    if (match.Success)
                    {
                        player.PlayerId = match.Groups[1].Value;
                    }
                }

                if (string.IsNullOrEmpty(player.HeadshotUrl))
                {
                    player.HeadshotUrl = "https://a.espncdn.com/i/headshots/nba/players/full/0.png";
                }

                players.Add(player);
            }

            return players;
        }
    }
}