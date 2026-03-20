namespace NBA_Website.Models
{
    public class CalendarViewModel
    {
        public List<CalendarGame> PlayedGames { get; set; } = new();
        public List<CalendarGame> UpcomingGames { get; set; } = new();
        public List<CalendarGame> PostponedGames { get; set; } = new(); 
        public DateTime CurrentDate { get; set; }
        public int TotalGames { get; set; }
        public int PlayedGamesCount { get; set; }
        public int UpcomingGamesCount { get; set; }
        public int PostponedGamesCount { get; set; } 
    }

    public class CalendarGame
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string TimeFormatted { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public bool IsTimeValid { get; set; }
        public bool IsGamePlayed { get; set; }
        public bool IsPostponed { get; set; } 
        public string GameStatus { get; set; } = string.Empty;
        public string GameStatusDetail { get; set; } = string.Empty; 

        public string Venue { get; set; } = string.Empty;
        public string VenueCity { get; set; } = string.Empty;
        public string VenueState { get; set; } = string.Empty;
        public int? Attendance { get; set; }

        public CalendarGameTeam HomeTeam { get; set; } = new();
        public CalendarGameTeam AwayTeam { get; set; } = new();

        public string HomeScore { get; set; } = string.Empty;
        public string AwayScore { get; set; } = string.Empty;
    }

    public class CalendarGameTeam
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ShortDisplayName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public bool IsHome { get; set; }
        public bool IsWinner { get; set; }
        public string Score { get; set; } = string.Empty;
    }
}