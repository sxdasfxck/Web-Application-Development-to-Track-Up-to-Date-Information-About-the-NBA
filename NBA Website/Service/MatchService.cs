using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class MatchService : InterfaceMatchService
    {
        private readonly HttpClient _httpClient;

        public MatchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<MatchViewModel?> GetMatchAsync(string eventId)
        {
            try
            {
                var url = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/summary?event={eventId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var result = new MatchViewModel
                {
                    Id = eventId,
                    HomePlayers = new List<MatchPlayer>(),
                    AwayPlayers = new List<MatchPlayer>()
                };

                result.HomeTeam = new MatchTeam { IsHome = true };
                result.AwayTeam = new MatchTeam { IsHome = false };

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("header", out var header))
                    {
                        if (header.TryGetProperty("competitions", out var competitions) && competitions.GetArrayLength() > 0)
                        {
                            var competition = competitions[0];

                            if (competition.TryGetProperty("date", out var date) && date.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(date.GetString(), out var dt))
                                {
                                    result.Date = dt;
                                    result.TimeFormatted = dt.ToString("HH:mm");
                                }
                            }

                            bool venueFound = false;

                            if (competition.TryGetProperty("venue", out var venue))
                            {
                                if (venue.ValueKind == JsonValueKind.Object)
                                {
                                    if (venue.TryGetProperty("fullName", out var fullName) && fullName.ValueKind == JsonValueKind.String)
                                    {
                                        result.Venue = fullName.GetString() ?? "";
                                        venueFound = true;
                                    }

                                    if (!venueFound)
                                    {
                                        if (venue.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                                        {
                                            result.Venue = name.GetString() ?? "";
                                            venueFound = true;
                                        }
                                    }
                                }
                                else if (venue.ValueKind == JsonValueKind.String)
                                {
                                    result.Venue = venue.GetString() ?? "";
                                    venueFound = true;
                                }
                            }

                            if (!venueFound && root.TryGetProperty("gameInfo", out var gameInfo))
                            {
                                if (gameInfo.TryGetProperty("venue", out var gameInfoVenue) && gameInfoVenue.ValueKind == JsonValueKind.Object)
                                {
                                    if (gameInfoVenue.TryGetProperty("fullName", out var fullName) && fullName.ValueKind == JsonValueKind.String)
                                    {
                                        result.Venue = fullName.GetString() ?? "";
                                        venueFound = true;
                                    }
                                }
                            }

                            if (!venueFound && result.HomeTeam != null)
                            {
                                var arenaByTeam = new Dictionary<string, string>
                                {
                                    { "LAL", "Crypto.com Arena" },
                                    { "LAC", "Crypto.com Arena" },
                                    { "DEN", "Ball Arena" },
                                    { "GSW", "Chase Center" },
                                    { "PHX", "Footprint Center" },
                                    { "SAC", "Golden 1 Center" },
                                    { "DAL", "American Airlines Center" },
                                    { "HOU", "Toyota Center" },
                                    { "MEM", "FedExForum" },
                                    { "NOP", "Smoothie King Center" },
                                    { "SAS", "Frost Bank Center" },
                                    { "OKC", "Paycom Center" },
                                    { "MIN", "Target Center" },
                                    { "UTA", "Delta Center" },
                                    { "POR", "Moda Center" },
                                    { "BOS", "TD Garden" },
                                    { "BKN", "Barclays Center" },
                                    { "NYK", "Madison Square Garden" },
                                    { "PHI", "Wells Fargo Center" },
                                    { "TOR", "Scotiabank Arena" },
                                    { "CHI", "United Center" },
                                    { "CLE", "Rocket Mortgage FieldHouse" },
                                    { "DET", "Little Caesars Arena" },
                                    { "IND", "Gainbridge Fieldhouse" },
                                    { "MIL", "Fiserv Forum" },
                                    { "ATL", "State Farm Arena" },
                                    { "CHA", "Spectrum Center" },
                                    { "MIA", "Kaseya Center" },
                                    { "ORL", "Kia Center" },
                                    { "WAS", "Capital One Arena" }
                                };

                                if (arenaByTeam.TryGetValue(result.HomeTeam.Abbreviation ?? "", out var arena))
                                {
                                    result.Venue = arena;
                                }
                            }

                            if (competition.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Object)
                            {
                                if (status.TryGetProperty("type", out var statusType) && statusType.ValueKind == JsonValueKind.Object)
                                {
                                    if (statusType.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
                                        result.GameStatus = desc.GetString() ?? "";

                                    if (statusType.TryGetProperty("id", out var statusId) && statusId.ValueKind == JsonValueKind.String)
                                    {
                                        result.IsGamePlayed = statusId.GetString() == "3";
                                    }
                                }
                            }

                            if (competition.TryGetProperty("competitors", out var competitors))
                            {
                                foreach (var comp in competitors.EnumerateArray())
                                {
                                    var isHome = false;
                                    if (comp.TryGetProperty("homeAway", out var homeAway) && homeAway.ValueKind == JsonValueKind.String)
                                        isHome = homeAway.GetString() == "home";

                                    var matchTeam = isHome ? result.HomeTeam : result.AwayTeam;
                                    matchTeam.IsHome = isHome;

                                    if (comp.TryGetProperty("winner", out var winner))
                                        matchTeam.IsWinner = winner.GetBoolean();

                                    if (comp.TryGetProperty("score", out var score))
                                    {
                                        if (score.ValueKind == JsonValueKind.Object)
                                        {
                                            if (score.TryGetProperty("displayValue", out var displayValue) && displayValue.ValueKind == JsonValueKind.String)
                                            {
                                                matchTeam.Score = displayValue.GetString() ?? "";
                                            }
                                            else if (score.TryGetProperty("value", out var value))
                                            {
                                                matchTeam.Score = value.ToString();
                                            }
                                        }
                                        else if (score.ValueKind == JsonValueKind.String)
                                        {
                                            matchTeam.Score = score.GetString() ?? "";
                                        }
                                        else if (score.ValueKind == JsonValueKind.Number)
                                        {
                                            matchTeam.Score = score.GetInt32().ToString();
                                        }
                                    }

                                    if (comp.TryGetProperty("team", out var teamInfo))
                                    {
                                        if (teamInfo.TryGetProperty("id", out var id))
                                            matchTeam.Id = int.Parse(id.GetString() ?? "0");

                                        if (teamInfo.TryGetProperty("abbreviation", out var abbrev))
                                            matchTeam.Abbreviation = abbrev.GetString() ?? "";

                                        if (teamInfo.TryGetProperty("displayName", out var displayName))
                                            matchTeam.DisplayName = displayName.GetString() ?? "";

                                        if (teamInfo.TryGetProperty("shortDisplayName", out var shortName))
                                            matchTeam.ShortDisplayName = shortName.GetString() ?? "";

                                        if (teamInfo.TryGetProperty("location", out var location))
                                            matchTeam.Location = location.GetString() ?? "";

                                        if (teamInfo.TryGetProperty("color", out var color))
                                            matchTeam.Color = color.GetString() ?? "000000";

                                        if (teamInfo.TryGetProperty("logo", out var logo) && logo.ValueKind == JsonValueKind.String)
                                            matchTeam.Logo = logo.GetString() ?? "";
                                    }
                                }
                            }
                        }
                    }

                    if (root.TryGetProperty("boxscore", out var boxscore))
                    {
                        if (boxscore.TryGetProperty("teams", out var teams))
                        {
                            foreach (var team in teams.EnumerateArray())
                            {
                                var isHome = false;
                                if (team.TryGetProperty("homeAway", out var homeAway))
                                    isHome = homeAway.GetString() == "home";

                                var targetTeam = isHome ? result.HomeTeam : result.AwayTeam;

                                if (team.TryGetProperty("statistics", out var statistics))
                                {
                                    foreach (var stat in statistics.EnumerateArray())
                                    {
                                        string statName = "";
                                        if (stat.TryGetProperty("label", out var label) && label.ValueKind == JsonValueKind.String)
                                        {
                                            statName = label.GetString() ?? "";
                                        }
                                        else if (stat.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                                        {
                                            statName = name.GetString() ?? "";
                                        }

                                        string statValue = "";
                                        if (stat.TryGetProperty("displayValue", out var displayValue) && displayValue.ValueKind == JsonValueKind.String)
                                        {
                                            statValue = displayValue.GetString() ?? "";
                                        }

                                        if (!string.IsNullOrEmpty(statName) && !string.IsNullOrEmpty(statValue))
                                        {
                                            targetTeam.Statistics[statName] = statValue;
                                        }
                                    }
                                }
                            }
                        }

                        if (boxscore.TryGetProperty("players", out var players))
                        {
                            foreach (var teamPlayers in players.EnumerateArray())
                            {
                                var isHome = false;
                                string? teamIdString = null;

                                if (teamPlayers.TryGetProperty("team", out var teamInfo))
                                {
                                    if (teamInfo.ValueKind == JsonValueKind.String)
                                    {
                                        teamIdString = teamInfo.GetString();
                                    }
                                    else if (teamInfo.TryGetProperty("id", out var teamId))
                                    {
                                        teamIdString = teamId.GetString();
                                    }

                                    if (teamIdString != null)
                                    {
                                        isHome = teamIdString == result.HomeTeam.Id.ToString();
                                    }
                                }

                                if (teamPlayers.TryGetProperty("statistics", out var statistics) && statistics.GetArrayLength() > 0)
                                {
                                    var playerStats = statistics[0];

                                    if (playerStats.TryGetProperty("athletes", out var athletes))
                                    {
                                        foreach (var athlete in athletes.EnumerateArray())
                                        {
                                            try
                                            {
                                                var player = new MatchPlayer();

                                                if (athlete.TryGetProperty("didNotPlay", out var didNotPlay))
                                                {
                                                    player.DidNotPlay = didNotPlay.GetBoolean();
                                                }

                                                if (athlete.TryGetProperty("athlete", out var athleteInfo) && athleteInfo.ValueKind == JsonValueKind.Object)
                                                {
                                                    if (athleteInfo.TryGetProperty("id", out var playerId))
                                                    {
                                                        if (playerId.ValueKind == JsonValueKind.String)
                                                            player.Id = int.Parse(playerId.GetString() ?? "0");
                                                        else if (playerId.ValueKind == JsonValueKind.Number)
                                                            player.Id = playerId.GetInt32();
                                                    }

                                                    if (athleteInfo.TryGetProperty("displayName", out var displayName) && displayName.ValueKind == JsonValueKind.String)
                                                        player.DisplayName = displayName.GetString() ?? "";

                                                    if (athleteInfo.TryGetProperty("shortName", out var shortName) && shortName.ValueKind == JsonValueKind.String)
                                                        player.ShortName = shortName.GetString() ?? "";

                                                    if (athleteInfo.TryGetProperty("jersey", out var jersey) && jersey.ValueKind == JsonValueKind.String)
                                                        player.Jersey = jersey.GetString() ?? "";

                                                    if (athleteInfo.TryGetProperty("position", out var position) && position.ValueKind == JsonValueKind.Object)
                                                    {
                                                        if (position.TryGetProperty("abbreviation", out var posAbbr) && posAbbr.ValueKind == JsonValueKind.String)
                                                            player.Position = posAbbr.GetString() ?? "";
                                                    }

                                                    if (athleteInfo.TryGetProperty("headshot", out var headshot) && headshot.ValueKind == JsonValueKind.Object)
                                                    {
                                                        if (headshot.TryGetProperty("href", out var href) && href.ValueKind == JsonValueKind.String)
                                                            player.HeadshotUrl = href.GetString() ?? "";
                                                    }
                                                }

                                                if (athlete.TryGetProperty("starter", out var starter))
                                                    player.IsStarter = starter.GetBoolean();

                                                if (athlete.TryGetProperty("stats", out var stats) && stats.GetArrayLength() > 0)
                                                {
                                                    var statsArray = stats.EnumerateArray().ToList();

                                                    for (int i = 0; i < statsArray.Count && i < 14; i++)
                                                    {
                                                        if (statsArray[i].ValueKind != JsonValueKind.String)
                                                            continue;

                                                        string value = statsArray[i].GetString() ?? "";

                                                        switch (i)
                                                        {
                                                            case 0: player.Minutes = value; break;
                                                            case 1: player.Points = value; break;
                                                            case 2: player.FieldGoals = value; break;
                                                            case 3: player.ThreePointers = value; break;
                                                            case 4: player.FreeThrows = value; break;
                                                            case 5: player.Rebounds = value; break;
                                                            case 6: player.Assists = value; break;
                                                            case 7: player.Turnovers = value; break;
                                                            case 8: player.Steals = value; break;
                                                            case 9: player.Blocks = value; break;
                                                            case 10: player.OffensiveRebounds = value; break;
                                                            case 11: player.DefensiveRebounds = value; break;
                                                            case 12: player.Fouls = value; break;
                                                            case 13: player.PlusMinus = value; break;
                                                        }
                                                    }
                                                }

                                                if (string.IsNullOrEmpty(player.HeadshotUrl) && player.Id > 0)
                                                {
                                                    player.HeadshotUrl = $"https://a.espncdn.com/i/headshots/nba/players/full/{player.Id}.png";
                                                }

                                                if (player.DidNotPlay || string.IsNullOrEmpty(player.Minutes) || player.Minutes == "0")
                                                {
                                                    player.DidNotPlay = true;
                                                    player.Minutes = "DNP";
                                                }

                                                if (!string.IsNullOrEmpty(player.DisplayName))
                                                {
                                                    if (isHome)
                                                    {
                                                        result.HomePlayers.Add(player);
                                                    }
                                                    else
                                                    {
                                                        result.AwayPlayers.Add(player);
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (root.TryGetProperty("leaders", out var leaders))
                    {
                        result.Leaders = new MatchLeaders();

                        foreach (var teamLeaders in leaders.EnumerateArray())
                        {
                            if (teamLeaders.TryGetProperty("team", out var team))
                            {
                                if (team.TryGetProperty("id", out var teamIdElement))
                                {
                                    var teamId = teamIdElement.GetString();
                                    var isHome = teamId == result.HomeTeam.Id.ToString();

                                    var teamLeadersObj = new TeamLeaders();

                                    if (teamLeaders.TryGetProperty("leaders", out var leadersArray))
                                    {
                                        foreach (var leader in leadersArray.EnumerateArray())
                                        {
                                            if (leader.TryGetProperty("name", out var leaderName))
                                            {
                                                var name = leaderName.GetString();
                                                if (leader.TryGetProperty("leaders", out var leadersList) && leadersList.GetArrayLength() > 0)
                                                {
                                                    var topLeader = leadersList[0];
                                                    if (topLeader.TryGetProperty("athlete", out var athlete) &&
                                                        topLeader.TryGetProperty("displayValue", out var displayValue))
                                                    {
                                                        if (athlete.TryGetProperty("id", out var athleteId) &&
                                                            athlete.TryGetProperty("displayName", out var athleteName))
                                                        {
                                                            var playerLeader = new PlayerLeader
                                                            {
                                                                PlayerId = int.Parse(athleteId.GetString() ?? "0"),
                                                                PlayerName = athleteName.GetString() ?? "",
                                                                Value = displayValue.GetString() ?? ""
                                                            };

                                                            switch (name)
                                                            {
                                                                case "points":
                                                                    teamLeadersObj.PointsLeader = playerLeader;
                                                                    break;
                                                                case "rebounds":
                                                                    teamLeadersObj.ReboundsLeader = playerLeader;
                                                                    break;
                                                                case "assists":
                                                                    teamLeadersObj.AssistsLeader = playerLeader;
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (isHome)
                                        result.Leaders.HomeTeamLeaders = teamLeadersObj;
                                    else
                                        result.Leaders.AwayTeamLeaders = teamLeadersObj;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(result.HomeTeam.Logo))
                    {
                        result.HomeTeam.Logo = $"https://a.espncdn.com/i/teamlogos/nba/500/{result.HomeTeam.Abbreviation?.ToLower() ?? "default"}.png";
                    }
                    if (string.IsNullOrEmpty(result.AwayTeam.Logo))
                    {
                        result.AwayTeam.Logo = $"https://a.espncdn.com/i/teamlogos/nba/500/{result.AwayTeam.Abbreviation?.ToLower() ?? "default"}.png";
                    }

                    if (string.IsNullOrEmpty(result.HomeTeam.ShortDisplayName))
                        result.HomeTeam.ShortDisplayName = result.HomeTeam.Abbreviation ?? "HOME";

                    if (string.IsNullOrEmpty(result.AwayTeam.ShortDisplayName))
                        result.AwayTeam.ShortDisplayName = result.AwayTeam.Abbreviation ?? "AWAY";
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}