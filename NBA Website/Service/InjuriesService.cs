using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class InjuriesService : InterfaceInjuriesService
    {
        private readonly HttpClient _httpClient;

        public InjuriesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<InjuriesViewModel?> GetTeamInjuriesAsync(string teamAbbrev)
        {
            try
            {
                var injuriesUrl = "https://site.api.espn.com/apis/site/v2/sports/basketball/nba/injuries";
                var response = await _httpClient.GetAsync(injuriesUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = new InjuriesViewModel
                {
                    TeamAbbrev = teamAbbrev.ToUpper(),
                    TeamLogo = GetTeamLogo(teamAbbrev),
                    InjuredPlayers = new List<InjuredPlayer>()
                };

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("injuries", out var injuriesArray))
                    {
                        foreach (var teamInjuries in injuriesArray.EnumerateArray())
                        {
                            if (teamInjuries.TryGetProperty("id", out var teamIdElement))
                            {
                                string teamId = teamIdElement.GetString() ?? "";
                                string teamNameFromJson = teamInjuries.TryGetProperty("displayName", out var teamNameElement)
                                    ? teamNameElement.GetString() ?? ""
                                    : "";

                                if (teamInjuries.TryGetProperty("injuries", out var injuriesList))
                                {
                                    foreach (var injury in injuriesList.EnumerateArray())
                                    {
                                        if (injury.TryGetProperty("athlete", out var athlete))
                                        {
                                            if (athlete.TryGetProperty("team", out var playerTeam))
                                            {
                                                string playerTeamAbbrev = playerTeam.TryGetProperty("abbreviation", out var abbrev)
                                                    ? abbrev.GetString() ?? ""
                                                    : "";

                                                if (playerTeamAbbrev.Equals(teamAbbrev, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    var injuredPlayer = ParseInjuredPlayer(injury, athlete);

                                                    if (result.TeamId == 0 && playerTeam.TryGetProperty("id", out var teamIdProp))
                                                    {
                                                        result.TeamId = int.Parse(teamIdProp.GetString() ?? "0");
                                                        result.TeamName = playerTeam.TryGetProperty("displayName", out var teamDisplayName)
                                                            ? teamDisplayName.GetString() ?? ""
                                                            : teamNameFromJson;

                                                        if (playerTeam.TryGetProperty("color", out var color))
                                                            result.TeamColor = color.GetString() ?? "";
                                                    }

                                                    result.InjuredPlayers.Add(injuredPlayer);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.TotalInjuries = result.InjuredPlayers.Count;

                if (result.TeamId == 0)
                {
                    await LoadTeamInfoAsync(result, teamAbbrev);
                }

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private InjuredPlayer ParseInjuredPlayer(JsonElement injury, JsonElement athlete)
        {
            var player = new InjuredPlayer();

            if (athlete.TryGetProperty("headshot", out var headshot) && headshot.TryGetProperty("href", out var href))
            {
                player.HeadshotUrl = href.GetString() ?? "";

                var hrefString = player.HeadshotUrl;
                var match = System.Text.RegularExpressions.Regex.Match(hrefString, @"(\d+)\.png");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idFromUrl))
                {
                    player.Id = idFromUrl;
                }
                else
                {
                    player.Id = 0;
                }
            }
            else
            {
                player.HeadshotUrl = "https://a.espncdn.com/i/headshots/nba/players/full/0.png";
                player.Id = 0;
            }

            if (athlete.TryGetProperty("firstName", out var firstName))
                player.FirstName = firstName.GetString() ?? "";
            if (athlete.TryGetProperty("lastName", out var lastName))
                player.LastName = lastName.GetString() ?? "";
            if (athlete.TryGetProperty("displayName", out var displayName))
                player.DisplayName = displayName.GetString() ?? "";
            if (athlete.TryGetProperty("jersey", out var jersey))
                player.Jersey = jersey.GetString() ?? "-";

            if (athlete.TryGetProperty("position", out var pos))
            {
                if (pos.TryGetProperty("abbreviation", out var posAbbr))
                    player.Position = posAbbr.GetString() ?? "-";
                else if (pos.TryGetProperty("name", out var posName))
                    player.Position = posName.GetString() ?? "-";
            }

            if (injury.TryGetProperty("status", out var status))
                player.Status = status.GetString() ?? "Unknown";
            if (injury.TryGetProperty("shortComment", out var shortComment))
                player.ShortComment = shortComment.GetString() ?? "";
            if (injury.TryGetProperty("longComment", out var longComment))
                player.LongComment = longComment.GetString() ?? "";
            if (injury.TryGetProperty("date", out var date))
            {
                if (DateTime.TryParse(date.GetString(), out var dt))
                    player.LastUpdate = dt.ToString("dd.MM.yyyy HH:mm");
            }

            if (injury.TryGetProperty("details", out var details))
            {
                if (details.TryGetProperty("type", out var type))
                    player.InjuryType = type.GetString() ?? "";
                if (details.TryGetProperty("location", out var location))
                    player.InjuryLocation = location.GetString() ?? "";
                if (details.TryGetProperty("detail", out var detail))
                    player.InjuryDetail = detail.GetString() ?? "";
                if (details.TryGetProperty("side", out var side))
                    player.Side = side.GetString() ?? "";
                if (details.TryGetProperty("returnDate", out var returnDate))
                {
                    if (DateTime.TryParse(returnDate.GetString(), out var retDt))
                        player.ReturnDate = retDt.ToString("dd.MM.yyyy");
                }
            }

            return player;
        }

        private async Task LoadTeamInfoAsync(InjuriesViewModel result, string teamAbbrev)
        {
            try
            {
                var teamUrl = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{teamAbbrev}";
                var teamResponse = await _httpClient.GetAsync(teamUrl);

                if (teamResponse.IsSuccessStatusCode)
                {
                    var teamJson = await teamResponse.Content.ReadAsStringAsync();
                    using (JsonDocument teamDoc = JsonDocument.Parse(teamJson))
                    {
                        var teamRoot = teamDoc.RootElement;
                        var team = teamRoot.GetProperty("team");

                        result.TeamName = team.GetProperty("displayName").GetString() ?? "";
                        result.TeamColor = team.TryGetProperty("color", out var color) ? color.GetString() ?? "" : "";
                        result.TeamId = int.Parse(team.GetProperty("id").GetString() ?? "0");
                    }
                }
            }
            catch { }
        }

        private string GetTeamLogo(string abbrev)
        {
            return $"https://a.espncdn.com/i/teamlogos/nba/500/{abbrev.ToLower()}.png";
        }
    }
}