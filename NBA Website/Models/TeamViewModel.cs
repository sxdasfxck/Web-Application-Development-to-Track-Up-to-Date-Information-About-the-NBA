namespace NBA_Website.Models
{
    public class TeamViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Color { get; set; } = "000000";
        public string? AlternateColor { get; set; }
        public string? Abbreviation { get; set; }
    }
}