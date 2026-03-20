using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class RosterService : InterfaceRosterService
    {
        private readonly HttpClient _httpClient;

        public RosterService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<RosterViewModel?> GetRosterInfoAsync(string teamAbbrev)
        {
            try
            {
                var rosterUrl = $"https://site.api.espn.com/apis/site/v2/sports/basketball/nba/teams/{teamAbbrev}/roster";
                var rosterResponse = await _httpClient.GetAsync(rosterUrl);

                if (!rosterResponse.IsSuccessStatusCode)
                    return null;

                var rosterJson = await rosterResponse.Content.ReadAsStringAsync();

                var result = new RosterViewModel
                {
                    TeamAbbrev = teamAbbrev.ToUpper(),
                    TeamLogo = GetTeamLogo(teamAbbrev),
                    PlayerNames = new List<string>(),
                    PlayerPositions = new List<string>(),
                    PlayerNumbers = new List<string>(),
                    PlayerHeights = new List<string>(),
                    PlayerWeights = new List<string>(),
                    PlayerAges = new List<string>(),
                    PlayerExperiences = new List<string>(),
                    PlayerImages = new List<string>(),
                    PlayerCountries = new List<string>(),
                    PlayerCountryCodes = new List<string>(),
                    PlayerSalaries = new List<string>()
                };

                using (JsonDocument doc = JsonDocument.Parse(rosterJson))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("coach", out var coach))
                    {
                        if (coach.ValueKind == JsonValueKind.Array && coach.GetArrayLength() > 0)
                        {
                            var firstCoach = coach[0];
                            string firstName = firstCoach.TryGetProperty("firstName", out var fn) ? fn.GetString() : "";
                            string lastName = firstCoach.TryGetProperty("lastName", out var ln) ? ln.GetString() : "";
                            result.HeadCoach = $"{firstName} {lastName}".Trim();

                            if (string.IsNullOrEmpty(result.HeadCoach))
                            {
                                if (firstCoach.TryGetProperty("displayName", out var displayName))
                                    result.HeadCoach = displayName.GetString() ?? "Неизвестно";
                            }
                        }
                    }

                    if (root.TryGetProperty("team", out var team))
                    {
                        if (team.TryGetProperty("displayName", out var teamName))
                            result.TeamName = teamName.GetString() ?? "";

                        if (team.TryGetProperty("color", out var color))
                            result.TeamColor = color.GetString() ?? "";

                        if (team.TryGetProperty("id", out var teamId))
                            result.TeamId = int.Parse(teamId.GetString() ?? "0");
                    }

                    if (root.TryGetProperty("athletes", out var athletes))
                    {
                        foreach (var athlete in athletes.EnumerateArray())
                        {
                            string playerName = "Неизвестно";
                            if (athlete.TryGetProperty("displayName", out var name))
                                playerName = name.GetString() ?? "Неизвестно";
                            result.PlayerNames.Add(playerName);

                            string playerPosition = "-";
                            if (athlete.TryGetProperty("position", out var pos))
                            {
                                if (pos.TryGetProperty("abbreviation", out var posAbbr))
                                    playerPosition = posAbbr.GetString() ?? "-";
                                else if (pos.TryGetProperty("name", out var posName))
                                    playerPosition = posName.GetString() ?? "-";
                            }
                            result.PlayerPositions.Add(playerPosition);

                            string playerNumber = "-";
                            if (athlete.TryGetProperty("jersey", out var jersey))
                                playerNumber = jersey.GetString() ?? "-";
                            result.PlayerNumbers.Add(playerNumber);

                            string playerHeight = "-";
                            if (athlete.TryGetProperty("displayHeight", out var height))
                                playerHeight = height.GetString() ?? "-";
                            result.PlayerHeights.Add(playerHeight);

                            string playerWeight = "-";
                            if (athlete.TryGetProperty("displayWeight", out var weight))
                                playerWeight = weight.GetString() ?? "-";
                            result.PlayerWeights.Add(playerWeight);

                            string playerAge = "-";
                            if (athlete.TryGetProperty("age", out var age))
                                playerAge = age.GetInt32().ToString();
                            result.PlayerAges.Add(playerAge);

                            string playerExp = "-";
                            if (athlete.TryGetProperty("experience", out var exp))
                            {
                                if (exp.TryGetProperty("years", out var expYears))
                                {
                                    int years = expYears.GetInt32();
                                    playerExp = years > 0 ? years.ToString() : "1";
                                }
                            }
                            result.PlayerExperiences.Add(playerExp);

                            string playerImage = "https://a.espncdn.com/combiner/i?img=/i/headshots/nba/players/full/0.png&w=200&h=200";
                            if (athlete.TryGetProperty("id", out var playerId))
                            {
                                var id = playerId.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    playerImage = $"https://a.espncdn.com/i/headshots/nba/players/full/{id}.png";
                                }
                            }
                            result.PlayerImages.Add(playerImage);

                            string playerCountry = "";
                            string playerCountryCode = "";
                            if (athlete.TryGetProperty("birthPlace", out var birthPlace))
                            {
                                if (birthPlace.TryGetProperty("country", out var country))
                                {
                                    playerCountry = country.GetString() ?? "";
                                    playerCountryCode = GetCountryCode(playerCountry);
                                }
                            }
                            result.PlayerCountries.Add(playerCountry);
                            result.PlayerCountryCodes.Add(playerCountryCode);

                            string playerSalary = "";
                            if (athlete.TryGetProperty("contract", out var contract))
                            {
                                if (contract.TryGetProperty("salary", out var salary))
                                {
                                    double salaryValue = salary.GetDouble();
                                    if (salaryValue > 0)
                                    {
                                        playerSalary = $"${(salaryValue / 1000000).ToString("F1")}M";
                                    }
                                }
                            }
                            result.PlayerSalaries.Add(playerSalary);
                        }
                    }
                }

                result.TotalPlayers = result.PlayerNames.Count;

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в RosterService: {ex.Message}");
                return null;
            }
        }

        private string GetTeamLogo(string abbrev)
        {
            return $"https://a.espncdn.com/i/teamlogos/nba/500/{abbrev.ToLower()}.png";
        }

        private string GetCountryCode(string countryName)
        {
            if (string.IsNullOrEmpty(countryName))
                return "";

            var countryCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "USA", "us" },
        { "United States", "us" },
        { "United States of America", "us" },
        { "US", "us" },
        { "U.S.A.", "us" },
        { "America", "us" },
        { "Canada", "ca" },
        
        { "Australia", "au" },
        { "New Zealand", "nz" },
        
        { "France", "fr" },
        { "Germany", "de" },
        { "Spain", "es" },
        { "Italy", "it" },
        { "Greece", "gr" },
        { "Serbia", "rs" },
        { "Croatia", "hr" },
        { "Slovenia", "si" },
        { "Lithuania", "lt" },
        { "Latvia", "lv" },
        { "Estonia", "ee" },
        { "Finland", "fi" },
        { "Sweden", "se" },
        { "Norway", "no" },
        { "Denmark", "dk" },
        { "Netherlands", "nl" },
        { "Belgium", "be" },
        { "Switzerland", "ch" },
        { "Austria", "at" },
        { "Poland", "pl" },
        { "Czech Republic", "cz" },
        { "Czechia", "cz" },
        { "Slovakia", "sk" },
        { "Hungary", "hu" },
        { "Romania", "ro" },
        { "Bulgaria", "bg" },
        { "Turkey", "tr" },
        { "Russia", "ru" },
        { "Ukraine", "ua" },
        { "Belarus", "by" },
        
        { "Georgia", "ge" },
        { "Armenia", "am" },
        { "Azerbaijan", "az" },
        { "Kazakhstan", "kz" },
        
        { "China", "cn" },
        { "Japan", "jp" },
        { "South Korea", "kr" },
        { "Korea", "kr" },
        { "Philippines", "ph" },
        { "India", "in" },
        { "Israel", "il" },
        { "Iran", "ir" },
        { "Iraq", "iq" },
        { "Lebanon", "lb" },
        { "Jordan", "jo" },
        { "UAE", "ae" },
        { "United Arab Emirates", "ae" },
        { "Saudi Arabia", "sa" },
        { "Qatar", "qa" },
        { "Kuwait", "kw" },
        
        { "Nigeria", "ng" },
        { "Senegal", "sn" },
        { "Cameroon", "cm" },
        { "Ivory Coast", "ci" },
        { "Côte d'Ivoire", "ci" },
        { "South Africa", "za" },
        { "Angola", "ao" },
        { "Egypt", "eg" },
        { "Tunisia", "tn" },
        { "Morocco", "ma" },
        { "Algeria", "dz" },
        { "Ghana", "gh" },
        { "Mali", "ml" },
        { "Sudan", "sd" },
        { "Democratic Republic of Congo", "cd" },
        { "DR Congo", "cd" },
        { "Congo", "cd" },
        { "Congo DR", "cd" },
        { "Republic of Congo", "cg" },
        { "Congo Republic", "cg" },
        
        { "Brazil", "br" },
        { "Argentina", "ar" },
        { "Uruguay", "uy" },
        { "Venezuela", "ve" },
        { "Colombia", "co" },
        { "Mexico", "mx" },
        { "Dominican Republic", "do" },
        { "Puerto Rico", "pr" },
        { "Cuba", "cu" },
        { "Jamaica", "jm" },
        { "Bahamas", "bs" },
      
        { "England", "gb-eng" },
        { "Scotland", "gb-sct" },
        { "Wales", "gb-wls" },
        { "Northern Ireland", "gb-nir" },
        { "United Kingdom", "gb" },
        { "UK", "gb" },
        { "Great Britain", "gb" }
    };

            if (countryCodes.TryGetValue(countryName, out string? code))
                return code;

            foreach (var kvp in countryCodes)
            {
                if (countryName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains(countryName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return "";
        }
    }
}