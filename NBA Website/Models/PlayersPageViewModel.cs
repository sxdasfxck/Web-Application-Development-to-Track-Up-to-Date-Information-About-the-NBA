using System.Text.Json.Serialization;

namespace NBA_Website.Models
{
    public class PlayersPageViewModel
    {
        public List<PlayerItem>? Items { get; set; }
        public int? TotalCount { get; set; }
        public int? CurrentPage { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }

        public string? Id { get; set; }
        public string? FullName { get; set; }
        public string? DisplayName { get; set; }
        public string? ShortName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Age { get; set; }
        public string? BirthPlace { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public string? Jersey { get; set; }
        public string? Position { get; set; }
        public string? PositionAbbrev { get; set; }
        public string? HeadshotUrl { get; set; }
        public string? TeamAbbrev { get; set; }
        public int? ExperienceYears { get; set; }

        public string? DraftText { get; set; }
        public int? DraftYear { get; set; }
        public int? DraftRound { get; set; }
        public int? DraftPick { get; set; }

        public int? Salary { get; set; }
        public int? YearsRemaining { get; set; }
        public string? ContractDetail { get; set; }

        public PlayerStats? Stats { get; set; }
        public List<PlayerGameLog>? GameLog { get; set; }
    }

    public class PlayerItem
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? TeamAbbrev { get; set; }
        public string? Position { get; set; }
        public string? HeadshotUrl { get; set; }
    }

    public class PlayerStats
    {
        public int GamesPlayed { get; set; }
        public double Minutes { get; set; }
        public double Points { get; set; }
        public double Rebounds { get; set; }
        public double Assists { get; set; }
        public double Steals { get; set; }
        public double Blocks { get; set; }
        public double Turnovers { get; set; }
        public double Fgm { get; set; }
        public double Fga { get; set; }
        public double Fgpct { get; set; }
        public double ThreePm { get; set; }
        public double ThreePa { get; set; }
        public double ThreePpct { get; set; }
        public double Ftm { get; set; }
        public double Fta { get; set; }
        public double Ftpct { get; set; }

        public int TotalMinutes { get; set; }
        public int TotalPoints { get; set; }
        public int TotalRebounds { get; set; }
        public int TotalAssists { get; set; }
        public int TotalSteals { get; set; }
        public int TotalBlocks { get; set; }
        public int TotalTurnovers { get; set; }
        public int TotalFgm { get; set; }
        public int TotalFga { get; set; }
        public int TotalThreePm { get; set; }
        public int TotalThreePa { get; set; }
        public int TotalFtm { get; set; }
        public int TotalFta { get; set; }
    }

    public class PlayerGameLog
    {
        public string? GameId { get; set; }
        public string? Date { get; set; }
        public string? Opponent { get; set; }
        public string? OpponentAbbrev { get; set; }
        public string? Result { get; set; }
        public bool IsWin { get; set; }
        public int Minutes { get; set; }
        public int Points { get; set; }
        public int Rebounds { get; set; }
        public int Assists { get; set; }
        public int Steals { get; set; }
        public int Blocks { get; set; }
        public int Turnovers { get; set; }
        public int Fgm { get; set; }
        public int Fga { get; set; }
        public int ThreePm { get; set; }
        public int ThreePa { get; set; }
        public int Ftm { get; set; }
        public int Fta { get; set; }
        public int PlusMinus { get; set; }
    }
}