namespace NBA_Website.Models
{
    public class NewsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Published { get; set; } = DateTime.Now;
        public string? ImageUrl { get; set; }
        public string? ArticleUrl { get; set; } 
        public string? ApiUrl { get; set; } 
        public List<string> Teams { get; set; } = new List<string>();
    }
}