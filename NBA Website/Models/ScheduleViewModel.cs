namespace NBA_Website.Models
{
    public class ScheduleViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string TeamColor { get; set; } = string.Empty;
        public string TeamLogo { get; set; } = string.Empty;
        public string TeamRecord { get; set; } = string.Empty;

        public List<GameEvent> Events { get; set; } = new List<GameEvent>();
    }

    public class GameEvent
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public bool IsTimeValid { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string VenueCity { get; set; } = string.Empty;
        public string VenueState { get; set; } = string.Empty;
        public int? Attendance { get; set; }
        public GameTeam HomeTeam { get; set; } = new GameTeam();
        public GameTeam AwayTeam { get; set; } = new GameTeam();
        public bool IsGamePlayed { get; set; }
        public string Result { get; set; } = string.Empty; 
        public string HomeScore { get; set; } = string.Empty;
        public string AwayScore { get; set; } = string.Empty;
        public string GameStatus { get; set; } = string.Empty; 
        public string? BoxscoreUrl { get; set; }
        public string? RecapUrl { get; set; }
    }

    public class GameTeam
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ShortDisplayName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public bool IsHome { get; set; }
        public bool IsWinner { get; set; }
        public string Score { get; set; } = string.Empty;
    }
}