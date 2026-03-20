using NBA_Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NBA_Website.Services
{
    public class ArticleService : InterfaceArticleService
    {
        private readonly HttpClient _httpClient;

        public ArticleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<ArticleViewModel?> GetArticleByIdAsync(string articleId)
        {
            try
            {
                var url = $"https://content.core.api.espn.com/v1/sports/news/{articleId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseArticleFromJson(json);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private ArticleViewModel? ParseArticleFromJson(string json)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("headlines", out var headlines) &&
                        headlines.GetArrayLength() > 0)
                    {
                        var article = headlines[0];

                        var result = new ArticleViewModel
                        {
                            Id = GetStringValue(article, "id") ?? "",
                            Headline = GetStringValue(article, "headline") ?? "Без заголовка",
                            Description = GetStringValue(article, "description") ?? "",
                            Published = GetDateTimeValue(article, "published") ?? DateTime.Now,
                            ArticleUrl = GetArticleUrl(article)
                        };

                        if (article.TryGetProperty("story", out var story))
                        {
                            result.Content = FormatArticleText(story.GetString() ?? "");
                        }

                        if (article.TryGetProperty("categories", out var categories))
                        {
                            foreach (var category in categories.EnumerateArray())
                            {
                                if (category.TryGetProperty("type", out var type) &&
                                    type.GetString() == "team" &&
                                    category.TryGetProperty("description", out var teamName))
                                {
                                    result.Teams.Add(teamName.GetString() ?? "");
                                }
                            }
                        }

                        return result;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string FormatArticleText(string html)
        {
            var text = Regex.Replace(html, @"<[^>]+>", " ");

            text = Regex.Replace(text, @"\s+", " ");

            var paragraphs = new List<string>();

            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            var currentParagraph = new List<string>();
            int sentenceCount = 0;

            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                currentParagraph.Add(trimmed + ".");
                sentenceCount++;

                if (sentenceCount >= 4)
                {
                    paragraphs.Add(string.Join(" ", currentParagraph));
                    currentParagraph.Clear();
                    sentenceCount = 0;
                }
            }

            if (currentParagraph.Any())
            {
                paragraphs.Add(string.Join(" ", currentParagraph));
            }

            return string.Join("\n\n", paragraphs);
        }

        private string? GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
                else if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32().ToString();
            }
            return null;
        }

        private DateTime? GetDateTimeValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(prop.GetString(), out var date))
                    return date;
            }
            return null;
        }

        private string GetArticleUrl(JsonElement article)
        {
            if (article.TryGetProperty("links", out var links))
            {
                if (links.TryGetProperty("web", out var web) &&
                    web.TryGetProperty("href", out var href))
                {
                    return href.GetString() ?? "";
                }
            }

            if (article.TryGetProperty("id", out var id))
            {
                string idStr = id.ValueKind == JsonValueKind.Number ?
                    id.GetInt32().ToString() : id.GetString() ?? "";

                if (!string.IsNullOrEmpty(idStr))
                {
                    return $"https://www.espn.com/nba/story/_/id/{idStr}";
                }
            }

            return "https://www.espn.com/nba/";
        }
    }
}