namespace NBA_Website.Models
{
    public class LeadersViewModel
    {
        public List<PlayerLeaders> PointsLeaders { get; set; } = new();
        public List<PlayerLeaders> AssistsLeaders { get; set; } = new();
        public List<PlayerLeaders> ThreePointLeaders { get; set; } = new();
        public List<PlayerLeaders> ReboundsLeaders { get; set; } = new();
        public List<PlayerLeaders> BlocksLeaders { get; set; } = new();
        public List<PlayerLeaders> StealsLeaders { get; set; } = new();
    }

    public class PlayerLeaders
    {
        public string Rank { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string HeadshotUrl { get; set; } = string.Empty;
    }
}