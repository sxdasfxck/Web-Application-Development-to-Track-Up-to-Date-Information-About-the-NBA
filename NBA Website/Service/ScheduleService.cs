using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class ScheduleService : InterfaceScheduleService
    {
        private readonly HttpClient _httpClient;

        public ScheduleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<ScheduleViewModel?> GetTeamScheduleAsync(string teamAbbrev)
        {
            try
            {
                var scheduleUrl = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{teamAbbrev}/schedule";
                var response = await _httpClient.GetAsync(scheduleUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var result = new ScheduleViewModel
                {
                    TeamAbbrev = teamAbbrev.ToUpper(),
                    TeamLogo = GetTeamLogo(teamAbbrev),
                    Events = new List<GameEvent>()
                };

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("team", out var team))
                    {
                        if (team.TryGetProperty("displayName", out var teamName))
                            result.TeamName = teamName.GetString() ?? "";

                        if (team.TryGetProperty("color", out var color))
                            result.TeamColor = color.GetString() ?? "";

                        if (team.TryGetProperty("id", out var teamId))
                            result.TeamId = int.Parse(teamId.GetString() ?? "0");

                        if (team.TryGetProperty("recordSummary", out var record))
                            result.TeamRecord = record.GetString() ?? "";
                    }

                    if (root.TryGetProperty("events", out var events))
                    {
                        foreach (var eventItem in events.EnumerateArray())
                        {
                            var gameEvent = ParseGameEvent(eventItem, teamAbbrev);
                            result.Events.Add(gameEvent);
                        }
                    }
                }

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

        private GameEvent ParseGameEvent(JsonElement eventItem, string teamAbbrev)
        {
            var game = new GameEvent();

            if (eventItem.TryGetProperty("id", out var id))
                game.Id = id.GetString() ?? "";

            if (eventItem.TryGetProperty("date", out var date))
            {
                if (DateTime.TryParse(date.GetString(), out var dt))
                {
                    game.Date = dt;
                    game.DateFormatted = dt.ToString("dd.MM.yyyy HH:mm");
                }
            }

            if (eventItem.TryGetProperty("name", out var name))
                game.Name = name.GetString() ?? "";

            if (eventItem.TryGetProperty("shortName", out var shortName))
                game.ShortName = shortName.GetString() ?? "";

            if (eventItem.TryGetProperty("timeValid", out var timeValid))
                game.IsTimeValid = timeValid.GetBoolean();

            if (eventItem.TryGetProperty("competitions", out var competitions) && competitions.GetArrayLength() > 0)
            {
                var competition = competitions[0];

                if (competition.TryGetProperty("status", out var status))
                {
                    if (status.TryGetProperty("type", out var statusType))
                    {
                        if (statusType.TryGetProperty("description", out var statusDesc))
                            game.GameStatus = statusDesc.GetString() ?? "";
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

                if (competition.TryGetProperty("links", out var links))
                {
                    foreach (var link in links.EnumerateArray())
                    {
                        var rels = link.GetProperty("rel").EnumerateArray().Select(r => r.GetString() ?? "").ToList();
                        var href = link.TryGetProperty("href", out var linkHref) ? linkHref.GetString() : null;

                        if (rels.Contains("boxscore"))
                            game.BoxscoreUrl = href;
                        else if (rels.Contains("recap"))
                            game.RecapUrl = href;
                    }
                }

                if (competition.TryGetProperty("competitors", out var competitors))
                {
                    foreach (var comp in competitors.EnumerateArray())
                    {
                        var team = ParseGameTeam(comp);

                        if (team.IsHome)
                            game.HomeTeam = team;
                        else
                            game.AwayTeam = team;
                    }
                }

                bool isHomeTeam = game.HomeTeam.Abbreviation.Equals(teamAbbrev, StringComparison.OrdinalIgnoreCase);
                var ourTeam = isHomeTeam ? game.HomeTeam : game.AwayTeam;
                var opponent = isHomeTeam ? game.AwayTeam : game.HomeTeam;

                game.IsGamePlayed = !string.IsNullOrEmpty(ourTeam.Score) && ourTeam.Score != "0";

                if (game.IsGamePlayed)
                {
                    game.HomeScore = game.HomeTeam.Score;
                    game.AwayScore = game.AwayTeam.Score;
                    game.Result = ourTeam.IsWinner ? "W" : "L";
                }
            }

            return game;
        }

        private GameTeam ParseGameTeam(JsonElement competitor)
        {
            var team = new GameTeam();

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

                if (teamInfo.TryGetProperty("logos", out var logos) && logos.GetArrayLength() > 0)
                {
                    if (logos[0].TryGetProperty("href", out var logoHref))
                        team.Logo = logoHref.GetString() ?? "";
                }
            }

            return team;
        }

        private async Task LoadTeamInfoAsync(ScheduleViewModel result, string teamAbbrev)
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

                        if (team.TryGetProperty("recordSummary", out var record))
                            result.TeamRecord = record.GetString() ?? "";
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