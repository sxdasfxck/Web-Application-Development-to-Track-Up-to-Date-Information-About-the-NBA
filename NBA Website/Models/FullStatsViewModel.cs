namespace NBA_Website.Models
{
    public class FullStatsViewModel
    {
        public List<PlayerFullStats> Players { get; set; } = new();
        public string SortBy { get; set; } = "PTS";
        public string StatsType { get; set; } = "main";
        public int TotalPlayers { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => (int)Math.Ceiling(TotalPlayers / (double)PageSize);
    }
    
    public class PlayerFullStats
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public double Minutes { get; set; }
        public double Points { get; set; }
        public double FGM { get; set; }
        public double FGA { get; set; }
        public double FGPct { get; set; }
        public double ThreePM { get; set; }
        public double ThreePA { get; set; }
        public double ThreePPct { get; set; }
        public double FTM { get; set; }
        public double FTA { get; set; }
        public double FTPct { get; set; }
        public double Rebounds { get; set; }
        public double Assists { get; set; }
        public double Steals { get; set; }
        public double Blocks { get; set; }
        public double Turnovers { get; set; }
        public int DoubleDoubles { get; set; }
        public int TripleDoubles { get; set; }
        public string HeadshotUrl { get; set; } = string.Empty;
    }
}