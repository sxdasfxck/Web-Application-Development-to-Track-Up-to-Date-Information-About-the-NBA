using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class TeamDetailService : InterfaceTeamDetailService
    {
        private readonly HttpClient _httpClient;

        public TeamDetailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<TeamDetailViewModel?> GetTeamDetailAsync(string abbreviation)
        {
            try
            {
                var url = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{abbreviation}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseTeamDetailFromJson(json);
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private TeamDetailViewModel? ParseTeamDetailFromJson(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var team = root.GetProperty("team");

                    var result = new TeamDetailViewModel
                    {
                        Id = int.Parse(team.GetProperty("id").GetString()),
                        Abbreviation = team.GetProperty("abbreviation").GetString() ?? "",
                        DisplayName = team.GetProperty("displayName").GetString() ?? "",
                        Location = team.GetProperty("location").GetString() ?? "",
                        Color = team.TryGetProperty("color", out var color) ? color.GetString() ?? "" : "",
                        AlternateColor = team.TryGetProperty("alternateColor", out var altColor) ? altColor.GetString() ?? "" : ""
                    };

                    if (team.TryGetProperty("logos", out var logos) && logos.GetArrayLength() > 0)
                    {
                        result.LogoUrl = logos[0].GetProperty("href").GetString();
                    }

                    if (team.TryGetProperty("record", out var record) &&
                        record.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            if (item.TryGetProperty("type", out var type) &&
                                type.GetString() == "total" &&
                                item.TryGetProperty("stats", out var stats))
                            {
                                foreach (var stat in stats.EnumerateArray())
                                {
                                    var name = stat.GetProperty("name").GetString();
                                    var value = stat.GetProperty("value").GetDouble();

                                    switch (name)
                                    {
                                        case "wins":
                                            result.Wins = (int)value;
                                            break;
                                        case "losses":
                                            result.Losses = (int)value;
                                            break;
                                        case "winPercent":
                                            result.WinPct = value;
                                            break;
                                        case "avgPointsFor":
                                            result.PPG = value;
                                            break;
                                        case "avgPointsAgainst":
                                            result.OppPPG = value;
                                            break;
                                        case "differential":
                                            result.Diff = value;
                                            break;
                                        case "streak":
                                            result.Streak = value > 0 ? $"W{value}" : $"L{Math.Abs(value)}";
                                            break;
                                        case "playoffSeed":
                                            result.PlayoffSeed = (int)value;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    if (team.TryGetProperty("links", out var links))
                    {
                        var orderedRels = new[] { "roster", "depthchart", "injuries", "schedule" };

                        var foundLinks = new Dictionary<string, TeamLink>();

                        foreach (var link in links.EnumerateArray())
                        {
                            var rels = link.GetProperty("rel").EnumerateArray()
                                          .Select(r => r.GetString() ?? "")
                                          .ToList();

                            var matchedRel = orderedRels.FirstOrDefault(r => rels.Contains(r));

                            if (matchedRel != null)
                            {
                                foundLinks[matchedRel] = new TeamLink
                                {
                                    Text = link.GetProperty("text").GetString() ?? "",
                                    ShortText = link.GetProperty("shortText").GetString() ?? "",
                                    Rel = rels,
                                    Href = link.TryGetProperty("href", out var href) ? href.GetString() : null
                                };
                            }
                        }

                        foreach (var rel in orderedRels)
                        {
                            if (foundLinks.TryGetValue(rel, out var link))
                            {
                                result.Links.Add(link);
                            }
                        }
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}