namespace NBA_Website.Models
{
    public class TeamDetailViewModel
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string AlternateColor { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinPct { get; set; }
        public double PPG { get; set; }
        public double OppPPG { get; set; }
        public double Diff { get; set; }
        public string? Streak { get; set; }
        public int PlayoffSeed { get; set; }

        public List<TeamLink> Links { get; set; } = new List<TeamLink>();
    }

    public class TeamLink
    {
        public string Text { get; set; } = string.Empty;
        public string ShortText { get; set; } = string.Empty;
        public List<string> Rel { get; set; } = new List<string>();
        public string? Href { get; set; }
    }
}