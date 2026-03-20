using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace NBA_Website.Services
{
    public class PlayersPageService : InterfacePlayersPageService
    {
        private readonly HttpClient _httpClient;
        private const string BaseApiUrl = "https://sports.core.api.espn.com/v2/sports/basketball/leagues/nba/athletes";

        public PlayersPageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<PlayersPageViewModel?> GetPlayersListAsync(int page = 1)
        {
            try
            {
                var url = $"{BaseApiUrl}?page={page}";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

                var result = new PlayersPageViewModel
                {
                    TotalCount = response.GetProperty("count").GetInt32(),
                    CurrentPage = response.GetProperty("pageIndex").GetInt32(),
                    PageSize = response.GetProperty("pageSize").GetInt32(),
                    TotalPages = response.GetProperty("pageCount").GetInt32(),
                    Items = new List<PlayerItem>()
                };

                var items = response.GetProperty("items").EnumerateArray();
                foreach (var item in items)
                {
                    var refUrl = item.GetProperty("$ref").GetString();
                    if (!string.IsNullOrEmpty(refUrl))
                    {
                        var playerId = ExtractPlayerId(refUrl);
                        var playerDetails = await GetPlayerDetailsAsync(playerId);
                        if (playerDetails != null)
                        {
                            result.Items.Add(new PlayerItem
                            {
                                Id = playerId,
                                Name = playerDetails.DisplayName ?? playerDetails.FullName,
                                TeamAbbrev = playerDetails.TeamAbbrev,
                                Position = playerDetails.PositionAbbrev,
                                HeadshotUrl = playerDetails.HeadshotUrl
                            });
                        }
                    }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<PlayersPageViewModel?> GetPlayerDetailsAsync(string playerId)
        {
            try
            {
                var url = $"{BaseApiUrl}/{playerId}?lang=en&region=us";
                var player = await _httpClient.GetFromJsonAsync<JsonElement>(url);

                var result = new PlayersPageViewModel
                {
                    Id = playerId,
                    FullName = GetString(player, "fullName"),
                    DisplayName = GetString(player, "displayName"),
                    ShortName = GetString(player, "shortName"),
                    FirstName = GetString(player, "firstName"),
                    LastName = GetString(player, "lastName"),
                    Age = GetInt(player, "age"),
                    Height = GetString(player, "displayHeight"),
                    Weight = GetString(player, "displayWeight"),
                    Jersey = GetString(player, "jersey"),
                    HeadshotUrl = GetNestedString(player, "headshot", "href"),
                    ExperienceYears = GetNestedInt(player, "experience", "years")
                };

                if (player.TryGetProperty("birthPlace", out var birthPlace))
                {
                    var city = GetString(birthPlace, "city");
                    var state = GetString(birthPlace, "state");
                    var country = GetString(birthPlace, "country");
                    result.BirthPlace = string.Join(", ", new[] { city, state, country }.Where(s => !string.IsNullOrEmpty(s)));
                }

                if (player.TryGetProperty("position", out var position))
                {
                    result.Position = GetString(position, "displayName");
                    result.PositionAbbrev = GetString(position, "abbreviation");
                }

                if (player.TryGetProperty("team", out var team))
                {
                    var teamRef = GetString(team, "$ref");
                    if (!string.IsNullOrEmpty(teamRef))
                    {
                        var teamIdMatch = Regex.Match(teamRef, @"teams/(\d+)");
                        if (teamIdMatch.Success)
                        {
                            result.TeamAbbrev = teamIdMatch.Groups[1].Value;
                        }
                    }
                }

                if (player.TryGetProperty("draft", out var draft))
                {
                    result.DraftText = GetString(draft, "displayText");
                    result.DraftYear = GetInt(draft, "year");
                    result.DraftRound = GetInt(draft, "round");
                    result.DraftPick = GetInt(draft, "selection");
                }

                if (player.TryGetProperty("contract", out var contract))
                {
                    result.Salary = GetInt(contract, "salary");
                    result.YearsRemaining = GetInt(contract, "yearsRemaining");
                    result.ContractDetail = result.Salary > 0 ? $"${result.Salary:N0}" : null;
                }

                await LoadPlayerSeasonDataAsync(result, playerId);

                return result;
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadPlayerSeasonDataAsync(PlayersPageViewModel player, string playerId)
        {
            try
            {
                var url = $"https://site.web.api.espn.com/apis/common/v3/sports/basketball/nba/athletes/{playerId}/gamelog";
                var gameLog = await _httpClient.GetFromJsonAsync<JsonElement>(url);

                var stats = new PlayerStats();
                var games = new List<PlayerGameLog>();

                if (gameLog.TryGetProperty("seasonTypes", out var seasonTypes) && seasonTypes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var seasonType in seasonTypes.EnumerateArray())
                    {
                        if (seasonType.TryGetProperty("displayName", out var displayName) &&
                            displayName.GetString() == "2025-26 Regular Season")
                        {
                            if (seasonType.TryGetProperty("summary", out var summary))
                            {
                                if (summary.TryGetProperty("stats", out var statsArray) && statsArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var statItem in statsArray.EnumerateArray())
                                    {
                                        if (statItem.TryGetProperty("type", out var type) &&
                                            statItem.TryGetProperty("stats", out var values) &&
                                            values.ValueKind == JsonValueKind.Array)
                                        {
                                            var statsList = values.EnumerateArray()
                                                .Select(s => s.GetString())
                                                .ToList();

                                            if (statsList.Count >= 14)
                                            {
                                                string statType = type.GetString();

                                                if (statType == "avg")
                                                {
                                                    stats.Minutes = double.Parse(statsList[0], CultureInfo.InvariantCulture);
                                                    stats.Points = double.Parse(statsList[13], CultureInfo.InvariantCulture);
                                                    stats.Rebounds = double.Parse(statsList[7], CultureInfo.InvariantCulture);
                                                    stats.Assists = double.Parse(statsList[8], CultureInfo.InvariantCulture);
                                                    stats.Steals = double.Parse(statsList[10], CultureInfo.InvariantCulture);
                                                    stats.Blocks = double.Parse(statsList[9], CultureInfo.InvariantCulture);
                                                    stats.Turnovers = double.Parse(statsList[12], CultureInfo.InvariantCulture);

                                                    var fg = statsList[1].Split('-');
                                                    stats.Fgm = double.Parse(fg[0], CultureInfo.InvariantCulture);
                                                    stats.Fga = double.Parse(fg[1], CultureInfo.InvariantCulture);
                                                    stats.Fgpct = double.Parse(statsList[2], CultureInfo.InvariantCulture);

                                                    var three = statsList[3].Split('-');
                                                    stats.ThreePm = double.Parse(three[0], CultureInfo.InvariantCulture);
                                                    stats.ThreePa = double.Parse(three[1], CultureInfo.InvariantCulture);
                                                    stats.ThreePpct = double.Parse(statsList[4], CultureInfo.InvariantCulture);

                                                    var ft = statsList[5].Split('-');
                                                    stats.Ftm = double.Parse(ft[0], CultureInfo.InvariantCulture);
                                                    stats.Fta = double.Parse(ft[1], CultureInfo.InvariantCulture);
                                                    stats.Ftpct = double.Parse(statsList[6], CultureInfo.InvariantCulture);
                                                }
                                                else if (statType == "total")
                                                {
                                                    stats.TotalMinutes = int.Parse(statsList[0], CultureInfo.InvariantCulture);
                                                    stats.TotalPoints = int.Parse(statsList[13], CultureInfo.InvariantCulture);
                                                    stats.TotalRebounds = int.Parse(statsList[7], CultureInfo.InvariantCulture);
                                                    stats.TotalAssists = int.Parse(statsList[8], CultureInfo.InvariantCulture);
                                                    stats.TotalSteals = int.Parse(statsList[10], CultureInfo.InvariantCulture);
                                                    stats.TotalBlocks = int.Parse(statsList[9], CultureInfo.InvariantCulture);
                                                    stats.TotalTurnovers = int.Parse(statsList[12], CultureInfo.InvariantCulture);

                                                    var fg = statsList[1].Split('-');
                                                    stats.TotalFgm = int.Parse(fg[0], CultureInfo.InvariantCulture);
                                                    stats.TotalFga = int.Parse(fg[1], CultureInfo.InvariantCulture);

                                                    var three = statsList[3].Split('-');
                                                    stats.TotalThreePm = int.Parse(three[0], CultureInfo.InvariantCulture);
                                                    stats.TotalThreePa = int.Parse(three[1], CultureInfo.InvariantCulture);

                                                    var ft = statsList[5].Split('-');
                                                    stats.TotalFtm = int.Parse(ft[0], CultureInfo.InvariantCulture);
                                                    stats.TotalFta = int.Parse(ft[1], CultureInfo.InvariantCulture);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (seasonType.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var category in categories.EnumerateArray())
                                {
                                    if (category.TryGetProperty("events", out var categoryEvents) && categoryEvents.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var eventItem in categoryEvents.EnumerateArray())
                                        {
                                            var log = new PlayerGameLog();

                                            if (eventItem.TryGetProperty("eventId", out var eventId) &&
                                                gameLog.TryGetProperty("events", out var topEvents) &&
                                                topEvents.ValueKind == JsonValueKind.Object &&
                                                topEvents.TryGetProperty(eventId.GetString(), out var eventData))
                                            {
                                                log.GameId = eventId.GetString();

                                                if (eventData.TryGetProperty("gameDate", out var gameDate))
                                                {
                                                    log.Date = gameDate.GetString();
                                                }

                                                if (eventData.TryGetProperty("opponent", out var opponent))
                                                {
                                                    log.Opponent = opponent.TryGetProperty("displayName", out var oppName) ? oppName.GetString() : "";
                                                    log.OpponentAbbrev = opponent.TryGetProperty("abbreviation", out var oppAbbrev) ? oppAbbrev.GetString() : "";
                                                }

                                                if (eventData.TryGetProperty("score", out var score))
                                                {
                                                    log.Result = score.GetString();
                                                }

                                                if (eventData.TryGetProperty("gameResult", out var gameResult))
                                                {
                                                    log.IsWin = gameResult.GetString() == "W";
                                                }
                                            }

                                            if (eventItem.TryGetProperty("stats", out var statsArray) && statsArray.ValueKind == JsonValueKind.Array)
                                            {
                                                var statsList = statsArray.EnumerateArray().Select(s => s.GetString()).ToList();

                                                if (statsList.Count >= 14)
                                                {
                                                    log.Minutes = int.Parse(statsList[0], CultureInfo.InvariantCulture);
                                                    log.Points = int.Parse(statsList[13], CultureInfo.InvariantCulture);
                                                    log.Rebounds = int.Parse(statsList[7], CultureInfo.InvariantCulture);
                                                    log.Assists = int.Parse(statsList[8], CultureInfo.InvariantCulture);
                                                    log.Steals = int.Parse(statsList[10], CultureInfo.InvariantCulture);
                                                    log.Blocks = int.Parse(statsList[9], CultureInfo.InvariantCulture);
                                                    log.Turnovers = int.Parse(statsList[12], CultureInfo.InvariantCulture);

                                                    var fg = statsList[1].Split('-');
                                                    log.Fgm = int.Parse(fg[0], CultureInfo.InvariantCulture);
                                                    log.Fga = int.Parse(fg[1], CultureInfo.InvariantCulture);

                                                    var three = statsList[3].Split('-');
                                                    log.ThreePm = int.Parse(three[0], CultureInfo.InvariantCulture);
                                                    log.ThreePa = int.Parse(three[1], CultureInfo.InvariantCulture);

                                                    var ft = statsList[5].Split('-');
                                                    log.Ftm = int.Parse(ft[0], CultureInfo.InvariantCulture);
                                                    log.Fta = int.Parse(ft[1], CultureInfo.InvariantCulture);
                                                }
                                            }

                                            if (log.OpponentAbbrev != "STARS" &&
                                                log.OpponentAbbrev != "STRIPES" &&
                                                log.OpponentAbbrev != "WORLD")
                                            {
                                                games.Add(log);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }

                stats.GamesPlayed = games.Count;
                player.Stats = stats;
                player.GameLog = games.OrderByDescending(x => DateTime.Parse(x.Date)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadPlayerSeasonDataAsync: {ex.Message}");
            }
        }

        private string ExtractPlayerId(string refUrl)
        {
            var match = Regex.Match(refUrl, @"athletes/(\d+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        private string? GetString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var prop) ? prop.GetString() : null;
        }

        private string? GetNestedString(JsonElement element, string outerProp, string innerProp)
        {
            if (element.TryGetProperty(outerProp, out var outer) && outer.TryGetProperty(innerProp, out var inner))
                return inner.GetString();
            return null;
        }

        private int? GetInt(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int val))
                    return val;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed))
                    return parsed;
            }
            return null;
        }

        private int? GetNestedInt(JsonElement element, string outerProp, string innerProp)
        {
            if (element.TryGetProperty(outerProp, out var outer) && outer.TryGetProperty(innerProp, out var inner))
            {
                if (inner.ValueKind == JsonValueKind.Number && inner.TryGetInt32(out int val))
                    return val;
                if (inner.ValueKind == JsonValueKind.String && int.TryParse(inner.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed))
                    return parsed;
            }
            return null;
        }
    }
}