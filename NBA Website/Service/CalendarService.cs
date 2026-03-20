using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Services
{
    public class CalendarService : InterfaceCalendarService
    {
        private readonly HttpClient _httpClient;
        private static List<CalendarGame>? _cachedGames;
        private static DateTime _cacheTime;
        private readonly string[] _teamAbbrevs = new[]
        {
            "ATL", "BOS", "CLE", "NOP", "CHI", "DAL", "DEN", "DET", "GSW", "HOU",
            "LAC", "LAL", "MIA", "MIL", "MIN", "BKN", "NYK", "ORL", "IND", "PHI",
            "PHX", "POR", "SAC", "SAS", "OKC", "TOR", "UTA", "MEM", "WAS", "CHA"
        };

        public CalendarService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<CalendarViewModel?> GetCalendarAsync()
        {
            try
            {
                if (_cachedGames == null || (DateTime.Now - _cacheTime).TotalMinutes > 30)
                {
                    await LoadAllGamesAsync();
                }
                else
                {

                }

                var now = DateTime.UtcNow;
                var allGames = _cachedGames ?? new List<CalendarGame>();

                var playedGames = new List<CalendarGame>();
                var upcomingGames = new List<CalendarGame>();
                var postponedGames = new List<CalendarGame>();

                foreach (var game in allGames)
                {
                    if (game.IsGamePlayed)
                    {
                        playedGames.Add(game);
                    }
                    else if (game.IsPostponed)
                    {
                        postponedGames.Add(game);
                    }
                    else
                    {
                        upcomingGames.Add(game);
                    }
                }

                playedGames = playedGames.OrderByDescending(g => g.Date).ToList();
                upcomingGames = upcomingGames.OrderBy(g => g.Date).ToList();
                postponedGames = postponedGames.OrderBy(g => g.Date).ToList();
                
                int totalActiveGames = playedGames.Count + upcomingGames.Count;

                var result = new CalendarViewModel
                {
                    PlayedGames = playedGames,
                    UpcomingGames = upcomingGames,
                    PostponedGames = postponedGames,
                    CurrentDate = now,
                    TotalGames = totalActiveGames,
                    PlayedGamesCount = playedGames.Count,
                    UpcomingGamesCount = upcomingGames.Count,
                    PostponedGamesCount = postponedGames.Count
                };

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task LoadAllGamesAsync()
        {
            var allGames = new Dictionary<string, CalendarGame>();

            var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };

            await Parallel.ForEachAsync(_teamAbbrevs, options, async (abbrev, cancellationToken) =>
            {
                var teamGames = await GetTeamScheduleAsync(abbrev);

                if (teamGames != null)
                {
                    lock (allGames)
                    {
                        foreach (var game in teamGames)
                        {
                            if (!allGames.ContainsKey(game.Id))
                            {
                                allGames[game.Id] = game;
                            }
                        }
                    }
                }
            });

            _cachedGames = allGames.Values.ToList();
            _cacheTime = DateTime.Now;
        }

        private async Task<List<CalendarGame>?> GetTeamScheduleAsync(string teamAbbrev)
        {
            try
            {
                var url = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{teamAbbrev}/schedule";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<CalendarGame>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var games = new List<CalendarGame>();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("events", out var events))
                    {
                        foreach (var eventItem in events.EnumerateArray())
                        {
                            var game = ParseCalendarGameFromEvent(eventItem);
                            if (game != null)
                            {
                                games.Add(game);
                            }
                        }
                    }
                }

                return games;
            }
            catch (Exception ex)
            {
                return new List<CalendarGame>();
            }
        }

        private CalendarGame? ParseCalendarGameFromEvent(JsonElement eventItem)
        {
            try
            {
                var game = new CalendarGame();

                if (eventItem.TryGetProperty("id", out var id))
                    game.Id = id.GetString() ?? "";

                if (eventItem.TryGetProperty("date", out var date))
                {
                    if (DateTime.TryParse(date.GetString(), out var dt))
                    {
                        game.Date = dt;
                        game.TimeFormatted = dt.ToString("HH:mm");
                    }
                }

                if (eventItem.TryGetProperty("name", out var name))
                    game.Name = name.GetString() ?? "";

                if (eventItem.TryGetProperty("shortName", out var shortName))
                    game.ShortName = shortName.GetString() ?? "";

                if (eventItem.TryGetProperty("competitions", out var competitions) && competitions.GetArrayLength() > 0)
                {
                    var competition = competitions[0];

                    if (competition.TryGetProperty("status", out var status))
                    {
                        if (status.TryGetProperty("type", out var statusType))
                        {
                            if (statusType.TryGetProperty("description", out var statusDesc))
                                game.GameStatus = statusDesc.GetString() ?? "";

                            if (statusType.TryGetProperty("detail", out var statusDetail))
                                game.GameStatusDetail = statusDetail.GetString() ?? "";

                            if (statusType.TryGetProperty("id", out var statusId))
                            {
                                var statusIdStr = statusId.GetString();
                                game.IsGamePlayed = statusIdStr == "3";

                                if (statusIdStr == "5" ||
                                    game.GameStatus.Contains("Postponed", StringComparison.OrdinalIgnoreCase) ||
                                    game.GameStatus.Contains("TBD", StringComparison.OrdinalIgnoreCase) ||
                                    game.GameStatus.Contains("PPD", StringComparison.OrdinalIgnoreCase))
                                {
                                    game.IsPostponed = true;
                                }
                            }
                        }
                    }

                    if (competition.TryGetProperty("venue", out var venue))
                    {
                        if (venue.TryGetProperty("fullName", out var venueName))
                            game.Venue = venueName.GetString() ?? "";

                        if (venue.TryGetProperty("address", out var address))
                        {
                            if (address.TryGetProperty("city", out var city))
                                game.VenueCity = city.GetString() ?? "";
                            if (address.TryGetProperty("state", out var state))
                                game.VenueState = state.GetString() ?? "";
                        }
                    }

                    if (competition.TryGetProperty("attendance", out var attendance))
                        game.Attendance = attendance.GetInt32();

                    if (competition.TryGetProperty("competitors", out var competitors))
                    {
                        foreach (var comp in competitors.EnumerateArray())
                        {
                            var team = ParseCalendarGameTeam(comp);

                            if (team.IsHome)
                                game.HomeTeam = team;
                            else
                                game.AwayTeam = team;
                        }
                    }

                    if (game.IsGamePlayed)
                    {
                        game.HomeScore = game.HomeTeam.Score;
                        game.AwayScore = game.AwayTeam.Score;
                    }
                }

                return game;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private CalendarGameTeam ParseCalendarGameTeam(JsonElement competitor)
        {
            var team = new CalendarGameTeam();

            if (competitor.TryGetProperty("homeAway", out var homeAway))
                team.IsHome = homeAway.GetString() == "home";

            if (competitor.TryGetProperty("winner", out var winner))
                team.IsWinner = winner.GetBoolean();

            if (competitor.TryGetProperty("score", out var score))
            {
                if (score.TryGetProperty("displayValue", out var scoreValue))
                    team.Score = scoreValue.GetString() ?? "";
            }

            if (competitor.TryGetProperty("team", out var teamInfo))
            {
                if (teamInfo.TryGetProperty("id", out var id))
                    team.Id = int.Parse(id.GetString() ?? "0");

                if (teamInfo.TryGetProperty("abbreviation", out var abbrev))
                    team.Abbreviation = abbrev.GetString() ?? "";

                if (teamInfo.TryGetProperty("displayName", out var displayName))
                    team.DisplayName = displayName.GetString() ?? "";

                if (teamInfo.TryGetProperty("shortDisplayName", out var shortName))
                    team.ShortDisplayName = shortName.GetString() ?? "";

                if (teamInfo.TryGetProperty("location", out var location))
                    team.Location = location.GetString() ?? "";

                if (teamInfo.TryGetProperty("color", out var color))
                    team.Color = color.GetString() ?? "000000";

                if (teamInfo.TryGetProperty("logo", out var logo))
                {
                    team.Logo = logo.GetString() ?? "";
                }
                else if (teamInfo.TryGetProperty("logos", out var logos) && logos.GetArrayLength() > 0)
                {
                    if (logos[0].TryGetProperty("href", out var logoHref))
                        team.Logo = logoHref.GetString() ?? "";
                }

                if (string.IsNullOrEmpty(team.Logo))
                {
                    team.Logo = $"https://a.espncdn.com/i/teamlogos/nba/500/{team.Abbreviation.ToLower()}.png";
                }
            }

            return team;
        }
        
        public void ClearCache()
        {
            _cachedGames = null;
            _cacheTime = DateTime.MinValue;
        }
    }
}