namespace NBA_Website.Models
{
    public class StandingViewModel
    {
        public string TeamName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string Conference { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinPct { get; set; }
        public double GamesBack { get; set; }
        public string HomeRecord { get; set; } = string.Empty;
        public string AwayRecord { get; set; } = string.Empty;
        public string DivisionRecord { get; set; } = string.Empty;
        public string ConferenceRecord { get; set; } = string.Empty;
        public double PPG { get; set; }
        public double OppPPG { get; set; }
        public double Diff { get; set; }
        public string Streak { get; set; } = string.Empty;
        public string Last10 { get; set; } = string.Empty;
    }
}