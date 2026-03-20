namespace NBA_Website.Models
{
    public class MatchViewModel
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string TimeFormatted { get; set; } = string.Empty;
        public string GameStatus { get; set; } = string.Empty;
        public bool IsGamePlayed { get; set; }

        public MatchTeam HomeTeam { get; set; } = new();
        public MatchTeam AwayTeam { get; set; } = new();

        public List<MatchPlayer> HomePlayers { get; set; } = new();
        public List<MatchPlayer> AwayPlayers { get; set; } = new();

        public string Venue { get; set; } = string.Empty;

        public MatchLeaders? Leaders { get; set; }
    }

    public class MatchTeam
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

        public Dictionary<string, string> Statistics { get; set; } = new();
    }

    public class MatchPlayer
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Jersey { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string HeadshotUrl { get; set; } = string.Empty;
        public bool IsStarter { get; set; }
        public string Minutes { get; set; } = string.Empty;
        public string Points { get; set; } = string.Empty;
        public string FieldGoals { get; set; } = string.Empty;
        public string ThreePointers { get; set; } = string.Empty;
        public string FreeThrows { get; set; } = string.Empty;
        public string Rebounds { get; set; } = string.Empty;
        public string Assists { get; set; } = string.Empty;
        public string Turnovers { get; set; } = string.Empty;
        public string Steals { get; set; } = string.Empty;
        public string Blocks { get; set; } = string.Empty;
        public string OffensiveRebounds { get; set; } = string.Empty;
        public string DefensiveRebounds { get; set; } = string.Empty;
        public string Fouls { get; set; } = string.Empty;
        public string PlusMinus { get; set; } = string.Empty;
        public bool DidNotPlay { get; set; }
    }

    public class MatchLeaders
    {
        public TeamLeaders? HomeTeamLeaders { get; set; }
        public TeamLeaders? AwayTeamLeaders { get; set; }
    }

    public class TeamLeaders
    {
        public PlayerLeader? PointsLeader { get; set; }
        public PlayerLeader? ReboundsLeader { get; set; }
        public PlayerLeader? AssistsLeader { get; set; }
    }

    public class PlayerLeader
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}