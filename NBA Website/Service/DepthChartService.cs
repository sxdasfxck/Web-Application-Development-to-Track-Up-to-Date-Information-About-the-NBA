using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class DepthChartService : InterfaceDepthChartService
    {
        private readonly HttpClient _httpClient;

        public DepthChartService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<DepthChartViewModel?> GetTeamDepthChartAsync(string teamAbbrev)
        {
            try
            {
                var depthUrl = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{teamAbbrev}/depthcharts";

                var response = await _httpClient.GetAsync(depthUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var result = new DepthChartViewModel
                {
                    TeamAbbrev = teamAbbrev.ToUpper(),
                    TeamLogo = GetTeamLogo(teamAbbrev),
                    DepthChart = new Dictionary<string, PositionDepth>(),
                    PossibleLineups = new List<Lineup>()
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

                        if (team.TryGetProperty("location", out var location))
                            result.Location = location.GetString() ?? "";
                    }

                    if (root.TryGetProperty("depthchart", out var depthArray) && depthArray.GetArrayLength() > 0)
                    {
                        var firstChart = depthArray[0];

                        if (firstChart.TryGetProperty("positions", out var positions))
                        {
                            foreach (var position in positions.EnumerateObject())
                            {
                                var positionData = position.Value;
                                var positionKey = position.Name;

                                var positionDepth = new PositionDepth();

                                if (positionData.TryGetProperty("position", out var posInfo))
                                {
                                    if (posInfo.TryGetProperty("id", out var posId))
                                        positionDepth.PositionId = posId.GetString() ?? "";
                                    if (posInfo.TryGetProperty("name", out var posName))
                                        positionDepth.PositionName = posName.GetString() ?? "";
                                    if (posInfo.TryGetProperty("abbreviation", out var posAbbr))
                                        positionDepth.PositionAbbreviation = posAbbr.GetString() ?? "";
                                    if (posInfo.TryGetProperty("displayName", out var posDisplayName))
                                        positionDepth.PositionDisplayName = posDisplayName.GetString() ?? "";
                                }

                                if (positionData.TryGetProperty("athletes", out var athletes))
                                {
                                    int order = 1;
                                    foreach (var athlete in athletes.EnumerateArray())
                                    {
                                        var depthAthlete = ParseDepthAthlete(athlete, order);
                                        positionDepth.Athletes.Add(depthAthlete);
                                        order++;
                                    }
                                }

                                result.DepthChart[positionKey] = positionDepth;
                            }
                        }
                    }
                }

                result.PossibleLineups = GenerateSmartLineups(result.DepthChart);

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DepthAthlete ParseDepthAthlete(JsonElement athlete, int order)
        {
            var result = new DepthAthlete
            {
                Order = order
            };

            if (athlete.TryGetProperty("id", out var id))
                result.Id = int.Parse(id.GetString() ?? "0");

            if (athlete.TryGetProperty("displayName", out var displayName))
                result.DisplayName = displayName.GetString() ?? "";

            if (athlete.TryGetProperty("shortName", out var shortName))
                result.ShortName = shortName.GetString() ?? "";

            result.HeadshotUrl = $"https://a.espncdn.com/i/headshots/nba/players/full/{result.Id}.png";

            if (athlete.TryGetProperty("injuries", out var injuries) && injuries.GetArrayLength() > 0)
            {
                result.IsInjured = true;
            }

            return result;
        }

        private List<Lineup> GenerateSmartLineups(Dictionary<string, PositionDepth> depthChart)
        {
            var lineups = new List<Lineup>();

            var positionMapping = new Dictionary<string, List<string>>
    {
        { "pg", new List<string> { "pg", "sg", "g" } },
        { "sg", new List<string> { "sg", "pg", "sf", "g" } },
        { "sf", new List<string> { "sf", "sg", "pf", "f" } },
        { "pf", new List<string> { "pf", "sf", "c", "f" } },
        { "c", new List<string> { "c", "pf" } },
        { "g", new List<string> { "pg", "sg", "g" } },
        { "f", new List<string> { "sf", "pf", "f" } },
        { "pg/sg", new List<string> { "pg", "sg", "g" } },
        { "sf/pf", new List<string> { "sf", "pf", "f" } },
        { "pf/c", new List<string> { "pf", "c" } }
    };

            var requiredPositions = new[] { "pg", "sg", "sf", "pf", "c" };

            var allPlayers = new Dictionary<string, List<DepthAthlete>>();
            foreach (var pos in depthChart)
            {
                allPlayers[pos.Key] = new List<DepthAthlete>(pos.Value.Athletes);
            }

            var usedPlayerIds = new HashSet<int>();
            var lineupNumber = 1;

            while (true)
            {
                var currentLineup = new Lineup
                {
                    Name = GetLineupName(lineupNumber),
                    Players = new List<LineupPlayer>()
                };

                var lineupPlayerIds = new HashSet<int>();
                var playersForThisLineup = new Dictionary<string, DepthAthlete?>();

                foreach (var requiredPos in requiredPositions)
                {
                    DepthAthlete? selectedPlayer = null;
                    string selectedPosition = requiredPos;

                    if (allPlayers.ContainsKey(requiredPos))
                    {
                        selectedPlayer = allPlayers[requiredPos]
                            .FirstOrDefault(p => !usedPlayerIds.Contains(p.Id) && !lineupPlayerIds.Contains(p.Id));

                        if (selectedPlayer != null)
                        {
                            playersForThisLineup[requiredPos] = selectedPlayer;
                            lineupPlayerIds.Add(selectedPlayer.Id);
                            continue;
                        }
                    }

                    if (positionMapping.ContainsKey(requiredPos))
                    {
                        foreach (var altPos in positionMapping[requiredPos])
                        {
                            if (altPos == requiredPos) continue;

                            if (allPlayers.ContainsKey(altPos))
                            {
                                selectedPlayer = allPlayers[altPos]
                                    .FirstOrDefault(p => !usedPlayerIds.Contains(p.Id) && !lineupPlayerIds.Contains(p.Id));

                                if (selectedPlayer != null)
                                {
                                    selectedPosition = altPos;
                                    playersForThisLineup[requiredPos] = selectedPlayer;
                                    lineupPlayerIds.Add(selectedPlayer.Id);
                                    break;
                                }
                            }
                        }
                    }

                    if (selectedPlayer != null)
                    {
                        playersForThisLineup[requiredPos] = selectedPlayer;
                    }
                }

                if (playersForThisLineup.Count == 5)
                {
                    foreach (var pos in requiredPositions)
                    {
                        if (playersForThisLineup.ContainsKey(pos) && playersForThisLineup[pos] != null)
                        {
                            var player = playersForThisLineup[pos];
                            currentLineup.Players.Add(new LineupPlayer
                            {
                                Player = player,
                                Position = depthChart.ContainsKey(pos)
                                    ? depthChart[pos].PositionAbbreviation
                                    : pos.ToUpper()
                            });

                            usedPlayerIds.Add(player.Id);
                        }
                    }

                    if (currentLineup.Players.Count == 5)
                    {
                        lineups.Add(currentLineup);
                        lineupNumber++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break; 
                }

                if (lineupNumber > 10) break;
            }

            return lineups;
        }

        private string GetLineupName(int number)
        {
            return number switch
            {
                1 => "Стартовая пятерка",
                2 => "Вторая пятерка",
                3 => "Третья пятерка",
                4 => "Четвертая пятерка",
                5 => "Пятая пятерка",
                _ => $"Пятерка #{number}"
            };
        }

        private string GetTeamLogo(string abbrev)
        {
            return $"https://a.espncdn.com/i/teamlogos/nba/500/{abbrev.ToLower()}.png";
        }
    }
}