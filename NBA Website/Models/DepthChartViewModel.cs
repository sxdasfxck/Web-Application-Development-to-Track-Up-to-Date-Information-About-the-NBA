namespace NBA_Website.Models
{
    public class DepthChartViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string TeamColor { get; set; } = string.Empty;
        public string TeamLogo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public Dictionary<string, PositionDepth> DepthChart { get; set; } = new();
        public List<Lineup> PossibleLineups { get; set; } = new();
    }

    public class PositionDepth
    {
        public string PositionId { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string PositionAbbreviation { get; set; } = string.Empty;
        public string PositionDisplayName { get; set; } = string.Empty;
        public List<DepthAthlete> Athletes { get; set; } = new();
    }

    public class DepthAthlete
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string HeadshotUrl { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsInjured { get; set; }
    }

    public class Lineup
    {
        public string Name { get; set; } = string.Empty;
        public List<LineupPlayer> Players { get; set; } = new();
    }

    public class LineupPlayer
    {
        public DepthAthlete Player { get; set; } = new();
        public string Position { get; set; } = string.Empty;
    }
}