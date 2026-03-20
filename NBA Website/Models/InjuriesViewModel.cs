namespace NBA_Website.Models
{
    public class InjuriesViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamAbbrev { get; set; } = string.Empty;
        public string TeamColor { get; set; } = string.Empty;
        public string TeamLogo { get; set; } = string.Empty;

        public List<InjuredPlayer> InjuredPlayers { get; set; } = new List<InjuredPlayer>();
        public int TotalInjuries { get; set; }
    }

    public class InjuredPlayer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Jersey { get; set; } = string.Empty;
        public string HeadshotUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InjuryType { get; set; } = string.Empty;
        public string InjuryDetail { get; set; } = string.Empty;
        public string InjuryLocation { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public string ShortComment { get; set; } = string.Empty;
        public string LongComment { get; set; } = string.Empty;
        public string LastUpdate { get; set; } = string.Empty;
    }
}