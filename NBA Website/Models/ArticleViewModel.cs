namespace NBA_Website.Models
{
    public class ArticleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Published { get; set; }
        public string? ArticleUrl { get; set; }
        public List<string> Teams { get; set; } = new List<string>();
        public string Source { get; set; } = "ESPN";
    }
}