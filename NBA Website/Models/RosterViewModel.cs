using System.Text.Json;

namespace NBA_Website.Models
{
    public class RosterViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string TeamColor { get; set; } = string.Empty;
        public string TeamLogo { get; set; } = string.Empty;
        public string HeadCoach { get; set; } = string.Empty;

        public List<string> PlayerNames { get; set; } = new List<string>();
        public List<string> PlayerPositions { get; set; } = new List<string>();
        public List<string> PlayerNumbers { get; set; } = new List<string>();
        public List<string> PlayerHeights { get; set; } = new List<string>();
        public List<string> PlayerWeights { get; set; } = new List<string>();
        public List<string> PlayerAges { get; set; } = new List<string>();
        public List<string> PlayerExperiences { get; set; } = new List<string>();
        public List<string> PlayerImages { get; set; } = new List<string>();
        public List<string> PlayerCountries { get; set; } = new List<string>();
        public List<string> PlayerCountryCodes { get; set; } = new List<string>();
        public List<string> PlayerSalaries { get; set; } = new List<string>();

        public int TotalPlayers { get; set; }
    }
}