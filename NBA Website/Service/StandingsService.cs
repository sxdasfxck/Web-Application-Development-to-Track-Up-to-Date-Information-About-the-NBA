using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Services
{
    public class StandingsService : InterfaceStandingsService
    {
        private readonly HttpClient _httpClient;

        public StandingsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<StandingViewModel>> GetStandingsAsync()
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://www.espn.com/nba/standings");
                var json = ExtractJsonFromHtml(html);

                return string.IsNullOrEmpty(json) ? new List<StandingViewModel>() : ParseStandingsFromJson(json);
            }
            catch
            {
                return new List<StandingViewModel>();
            }
        }

        private string ExtractJsonFromHtml(string html)
        {
            var pattern = @"window\['__espnfitt__'\]\s*=\s*({.*?});\s*</script>";
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);

            return match.Success ? match.Groups[1].Value : null;
        }

        private List<StandingViewModel> ParseStandingsFromJson(string json)
        {
            var standings = new List<StandingViewModel>();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("page", out var page) &&
                    page.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("standings", out var standingsData) &&
                    standingsData.TryGetProperty("groups", out var groupsWrapper) &&
                    groupsWrapper.TryGetProperty("groups", out var groups))
                {
                    foreach (var group in groups.EnumerateArray())
                    {
                        var conferenceName = group.GetProperty("name").GetString();

                        if (group.TryGetProperty("standings", out var teams))
                        {
                            foreach (var teamData in teams.EnumerateArray())
                            {
                                var team = teamData.GetProperty("team");
                                var stats = teamData.GetProperty("stats");

                                standings.Add(new StandingViewModel
                                {
                                    TeamName = team.GetProperty("displayName").GetString(),
                                    TeamAbbrev = team.GetProperty("abbrev").GetString(),
                                    Conference = conferenceName,
                                    Wins = SafeParseInt(stats, 14),
                                    Losses = SafeParseInt(stats, 6),
                                    WinPct = SafeParseDouble(stats, 13),
                                    GamesBack = SafeParseGamesBack(stats, 4),
                                    HomeRecord = SafeGetString(stats, 17),
                                    AwayRecord = SafeGetString(stats, 18),
                                    DivisionRecord = SafeGetString(stats, 19),
                                    ConferenceRecord = SafeGetString(stats, 20),
                                    PPG = SafeParseDouble(stats, 1),
                                    OppPPG = SafeParseDouble(stats, 0),
                                    Diff = SafeParseDiff(stats, 2),
                                    Streak = SafeGetString(stats, 12),
                                    Last10 = SafeGetString(stats, 21)
                                });
                            }
                        }
                    }
                }
            }

            return standings;
        }

        private int SafeParseInt(JsonElement stats, int index)
        {
            if (index >= stats.GetArrayLength()) return 0;

            var value = stats[index].GetString();
            return string.IsNullOrEmpty(value) || value == "-" || value == "—" ? 0 :
                (int.TryParse(value, out int result) ? result : 0);
        }

        private double SafeParseDouble(JsonElement stats, int index)
        {
            if (index >= stats.GetArrayLength()) return 0.0;

            var value = stats[index].GetString();
            if (string.IsNullOrEmpty(value) || value == "-" || value == "—") return 0.0;

            value = value.Replace(",", ".");
            return double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0.0;
        }

        private double SafeParseGamesBack(JsonElement stats, int index)
        {
            if (index >= stats.GetArrayLength()) return 0.0;

            var value = stats[index].GetString();
            if (string.IsNullOrEmpty(value) || value == "-" || value == "—") return 0.0;

            value = value.Replace(",", ".");
            return double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0.0;
        }

        private double SafeParseDiff(JsonElement stats, int index)
        {
            if (index >= stats.GetArrayLength()) return 0.0;

            var value = stats[index].GetString();
            if (string.IsNullOrEmpty(value) || value == "-" || value == "—") return 0.0;

            value = value.Replace("+", "").Replace(",", ".");
            return double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0.0;
        }

        private string SafeGetString(JsonElement stats, int index)
        {
            if (index >= stats.GetArrayLength()) return "";
            return stats[index].GetString() ?? "";
        }
    }
}