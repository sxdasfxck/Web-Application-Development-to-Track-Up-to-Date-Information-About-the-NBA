using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Services
{
    public class FullStatsService : InterfaceFullStatsService
    {
        private readonly HttpClient _httpClient;
        private static List<PlayerFullStats>? _cachedPlayers;
        private static DateTime _cacheTime;
        private static Dictionary<string, int> _teamGamesPlayed = new();

        public FullStatsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<FullStatsViewModel?> GetFullStatsAsync(string sortBy = "PTS", string type = "main", int page = 1)
        {
            try
            {
                if (_cachedPlayers == null || (DateTime.Now - _cacheTime).TotalMinutes > 30)
                {
                    await LoadAllPlayersAsync();
                }

                var qualifiedPlayers = _cachedPlayers?
                    .Where(p => IsQualified(p, sortBy, type))
                    .ToList() ?? new();

                var sortedPlayers = SortPlayers(qualifiedPlayers, sortBy);

                var result = new FullStatsViewModel
                {
                    SortBy = sortBy,
                    StatsType = type,
                    CurrentPage = page,
                    TotalPlayers = sortedPlayers.Count,
                    Players = sortedPlayers
                        .Skip((page - 1) * 50)
                        .Take(50)
                        .ToList(),
                };

                return result;
            }
            catch
            {
                return null;
            }
        }

        private bool IsQualified(PlayerFullStats player, string sortBy, string statsType)
        {
            if (!_teamGamesPlayed.ContainsKey(player.Team))
                return false;

            var teamGames = _teamGamesPlayed[player.Team];
            var minGamesRequired = (int)(teamGames * 0.7);

            if (statsType == "shooting")
            {
                string cleanSortBy = sortBy.Replace("_desc", "");

                switch (cleanSortBy)
                {
                    case "FG%":
                        return player.FGA >= 0.1;
                    case "3P%":
                        return player.ThreePA >= 0.1;
                    case "FT%":
                        return player.FTA >= 0.1;
                    default:
                        return true;
                }
            }

            if (player.GamesPlayed < minGamesRequired)
                return false;

            string cleanMainSortBy = sortBy.Replace("_desc", "");

            switch (cleanMainSortBy)
            {
                case "FG%":
                    return player.FGA >= 0.1;
                case "3P%":
                    return player.ThreePA >= 0.1;
                case "FT%":
                    return player.FTA >= 0.1;
                default:
                    return true;
            }
        }

        private async Task LoadAllPlayersAsync()
        {
            var allPlayers = new List<PlayerFullStats>();
            int page = 1;
            int totalPages = 1;

            do
            {
                var url = page == 1
                    ? "https://www.espn.com/nba/stats/player"
                    : $"https://www.espn.com/nba/stats/player/_/page/{page}";

                var html = await _httpClient.GetStringAsync(url);
                var json = ExtractJsonFromHtml(html);

                if (string.IsNullOrEmpty(json))
                    break;

                var (players, pages, teamGames) = ParseStatsFromJson(json, page);
                allPlayers.AddRange(players);

                foreach (var team in teamGames)
                {
                    if (!_teamGamesPlayed.ContainsKey(team.Key))
                        _teamGamesPlayed[team.Key] = team.Value;
                }

                if (page == 1)
                    totalPages = pages;

                page++;
            } while (page <= totalPages);

            _cachedPlayers = allPlayers;
            _cacheTime = DateTime.Now;
        }

        private string ExtractJsonFromHtml(string html)
        {
            var pattern = @"window\['__espnfitt__'\]\s*=\s*({.*?});\s*</script>";
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : null;
        }

        private (List<PlayerFullStats> Players, int TotalPages, Dictionary<string, int> TeamGames) ParseStatsFromJson(string json, int currentPage)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                var players = new List<PlayerFullStats>();
                var teamGames = new Dictionary<string, int>();
                int totalPages = 1;

                if (root.TryGetProperty("page", out var page) &&
                    page.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("statistics", out var statistics))
                {
                    if (statistics.TryGetProperty("playerStats", out var playerStats))
                    {
                        int startRank = (currentPage - 1) * 50 + 1;
                        int rank = startRank;

                        foreach (var playerStat in playerStats.EnumerateArray())
                        {
                            var player = ParsePlayerStats(playerStat);
                            if (player != null)
                            {
                                players.Add(player);

                                if (!teamGames.ContainsKey(player.Team))
                                {
                                    teamGames[player.Team] = 65;
                                }
                                rank++;
                            }
                        }
                    }

                    if (statistics.TryGetProperty("metadata", out var metadata) &&
                        metadata.TryGetProperty("totalPages", out var pages))
                    {
                        totalPages = pages.GetInt32();
                    }
                }

                return (players, totalPages, teamGames);
            }
        }

        private PlayerFullStats? ParsePlayerStats(JsonElement playerStat)
        {
            try
            {
                if (!playerStat.TryGetProperty("athlete", out var athlete))
                    return null;

                var stats = new PlayerFullStats();

                if (athlete.TryGetProperty("id", out var id))
                {
                    stats.Id = id.GetString() ?? "";
                }
                else if (athlete.TryGetProperty("href", out var href))
                {
                    var hrefString = href.GetString() ?? "";
                    var playerIdMatch = Regex.Match(hrefString, @"id/(\d+)");
                    if (playerIdMatch.Success)
                    {
                        stats.Id = playerIdMatch.Groups[1].Value;
                    }
                }

                stats.Name = GetString(athlete, "name") ?? GetString(athlete, "shortName") ?? "";

                var teamValue = GetString(athlete, "team") ?? "";
                if (teamValue.Contains("/"))
                {
                    var teams = teamValue.Split('/');
                    stats.Team = teams[teams.Length - 1].Trim();
                }
                else
                {
                    stats.Team = teamValue;
                }

                stats.Position = GetString(athlete, "position") ?? "";

                if (athlete.TryGetProperty("href", out var href2))
                {
                    var hrefString = href2.GetString() ?? "";
                    var playerIdMatch = Regex.Match(hrefString, @"id/(\d+)");
                    if (playerIdMatch.Success)
                    {
                        var playerId = playerIdMatch.Groups[1].Value;
                        stats.HeadshotUrl = $"https://a.espncdn.com/i/headshots/nba/players/full/{playerId}.png";
                    }
                }

                if (string.IsNullOrEmpty(stats.HeadshotUrl))
                {
                    stats.HeadshotUrl = "https://a.espncdn.com/i/headshots/nba/players/full/0.png";
                }

                if (playerStat.TryGetProperty("stats", out var statsArray) &&
                    statsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var stat in statsArray.EnumerateArray())
                    {
                        if (!stat.TryGetProperty("name", out var name) ||
                            !stat.TryGetProperty("value", out var value))
                            continue;

                        var statName = name.GetString();
                        var statValue = value.GetString();

                        switch (statName)
                        {
                            case "gamesPlayed":
                                stats.GamesPlayed = ParseInt(statValue);
                                break;
                            case "avgMinutes":
                                stats.Minutes = ParseDouble(statValue);
                                break;
                            case "avgPoints":
                                stats.Points = ParseDouble(statValue);
                                break;
                            case "avgFieldGoalsMade":
                                stats.FGM = ParseDouble(statValue);
                                break;
                            case "avgFieldGoalsAttempted":
                                stats.FGA = ParseDouble(statValue);
                                break;
                            case "fieldGoalPct":
                                stats.FGPct = ParseDouble(statValue);
                                break;
                            case "avgThreePointFieldGoalsMade":
                                stats.ThreePM = ParseDouble(statValue);
                                break;
                            case "avgThreePointFieldGoalsAttempted":
                                stats.ThreePA = ParseDouble(statValue);
                                break;
                            case "threePointFieldGoalPct":
                                stats.ThreePPct = ParseDouble(statValue);
                                break;
                            case "avgFreeThrowsMade":
                                stats.FTM = ParseDouble(statValue);
                                break;
                            case "avgFreeThrowsAttempted":
                                stats.FTA = ParseDouble(statValue);
                                break;
                            case "freeThrowPct":
                                stats.FTPct = ParseDouble(statValue);
                                break;
                            case "avgRebounds":
                                stats.Rebounds = ParseDouble(statValue);
                                break;
                            case "avgAssists":
                                stats.Assists = ParseDouble(statValue);
                                break;
                            case "avgSteals":
                                stats.Steals = ParseDouble(statValue);
                                break;
                            case "avgBlocks":
                                stats.Blocks = ParseDouble(statValue);
                                break;
                            case "avgTurnovers":
                                stats.Turnovers = ParseDouble(statValue);
                                break;
                            case "doubleDouble":
                                stats.DoubleDoubles = ParseInt(statValue);
                                break;
                            case "tripleDouble":
                                stats.TripleDoubles = ParseInt(statValue);
                                break;
                        }
                    }
                }

                return stats;
            }
            catch
            {
                return null;
            }
        }

        private double ParseDouble(string? value)
        {
            if (string.IsNullOrEmpty(value) || value == "-" || value == "—") return 0;
            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return Math.Round(result, 1);
            return 0;
        }

        private int ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value) || value == "-" || value == "—") return 0;
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        private string GetString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var prop) ? prop.GetString() ?? "" : "";
        }

        private List<PlayerFullStats> SortPlayers(List<PlayerFullStats> players, string sortBy)
        {
            bool descending = !sortBy.EndsWith("_desc");
            string cleanSortBy = sortBy.Replace("_desc", "");

            return cleanSortBy switch
            {
                "PTS" => descending
                    ? players.OrderByDescending(p => p.Points).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Points).ThenByDescending(p => p.GamesPlayed).ToList(),

                "AST" => descending
                    ? players.OrderByDescending(p => p.Assists).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Assists).ThenByDescending(p => p.GamesPlayed).ToList(),

                "REB" => descending
                    ? players.OrderByDescending(p => p.Rebounds).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Rebounds).ThenByDescending(p => p.GamesPlayed).ToList(),

                "BLK" => descending
                    ? players.OrderByDescending(p => p.Blocks).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Blocks).ThenByDescending(p => p.GamesPlayed).ToList(),

                "STL" => descending
                    ? players.OrderByDescending(p => p.Steals).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Steals).ThenByDescending(p => p.GamesPlayed).ToList(),

                "3PM" => descending
                    ? players.OrderByDescending(p => p.ThreePM).ThenByDescending(p => p.ThreePA).ThenByDescending(p => p.ThreePPct).ToList()
                    : players.OrderBy(p => p.ThreePM).ThenByDescending(p => p.ThreePA).ThenByDescending(p => p.ThreePPct).ToList(),

                "3PA" => descending
                    ? players.OrderByDescending(p => p.ThreePA).ThenByDescending(p => p.ThreePM).ThenByDescending(p => p.ThreePPct).ToList()
                    : players.OrderBy(p => p.ThreePA).ThenByDescending(p => p.ThreePM).ThenByDescending(p => p.ThreePPct).ToList(),

                "3P%" => descending
                    ? players.OrderByDescending(p => p.ThreePPct).ThenByDescending(p => p.ThreePM).ThenByDescending(p => p.ThreePA).ToList()
                    : players.OrderBy(p => p.ThreePPct).ThenByDescending(p => p.ThreePM).ThenByDescending(p => p.ThreePA).ToList(),

                "FGM" => descending
                    ? players.OrderByDescending(p => p.FGM).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FGM).ThenByDescending(p => p.GamesPlayed).ToList(),

                "FGA" => descending
                    ? players.OrderByDescending(p => p.FGA).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FGA).ThenByDescending(p => p.GamesPlayed).ToList(),

                "FG%" => descending
                    ? players.OrderByDescending(p => p.FGPct).ThenByDescending(p => p.FGM).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FGPct).ThenByDescending(p => p.FGM).ThenByDescending(p => p.GamesPlayed).ToList(),

                "FTM" => descending
                    ? players.OrderByDescending(p => p.FTM).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FTM).ThenByDescending(p => p.GamesPlayed).ToList(),

                "FTA" => descending
                    ? players.OrderByDescending(p => p.FTA).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FTA).ThenByDescending(p => p.GamesPlayed).ToList(),

                "FT%" => descending
                    ? players.OrderByDescending(p => p.FTPct).ThenByDescending(p => p.FTM).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.FTPct).ThenByDescending(p => p.FTM).ThenByDescending(p => p.GamesPlayed).ToList(),

                "MIN" => descending
                    ? players.OrderByDescending(p => p.Minutes).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Minutes).ThenByDescending(p => p.GamesPlayed).ToList(),

                "TO" => descending
                    ? players.OrderByDescending(p => p.Turnovers).ThenByDescending(p => p.GamesPlayed).ToList()
                    : players.OrderBy(p => p.Turnovers).ThenByDescending(p => p.GamesPlayed).ToList(),

                "DD2" => descending
                    ? players.OrderByDescending(p => p.DoubleDoubles).ThenByDescending(p => p.Points).ToList()
                    : players.OrderBy(p => p.DoubleDoubles).ThenByDescending(p => p.Points).ToList(),

                "TD3" => descending
                    ? players.OrderByDescending(p => p.TripleDoubles).ThenByDescending(p => p.Points).ToList()
                    : players.OrderBy(p => p.TripleDoubles).ThenByDescending(p => p.Points).ToList(),

                _ => players.OrderByDescending(p => p.Points).ThenByDescending(p => p.GamesPlayed).ToList()
            };
        }
    }
}