using NBA_Website.Models;
using System.Text.Json;

namespace NBA_Website.Services
{
    public class ESPNService : InterfaceESPNService
    {
        private readonly HttpClient _httpClient;

        public ESPNService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://site.api.espn.com/apis/site/v2/sports/basketball/nba/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        private string GetBestImageUrl(JsonElement article)
        {
            if (!article.TryGetProperty("images", out var images) || images.GetArrayLength() == 0)
                return null;

            string bestImageUrl = null;
            int maxWidth = 0;
            int maxHeight = 0;

            foreach (var img in images.EnumerateArray())
            {
                try
                {
                    if (!img.TryGetProperty("url", out var url))
                        continue;

                    var currentUrl = url.GetString();
                    if (string.IsNullOrEmpty(currentUrl))
                        continue;

                    int currentWidth = 0;
                    int currentHeight = 0;

                    if (img.TryGetProperty("width", out var width))
                    {
                        currentWidth = width.GetInt32();
                    }

                    if (img.TryGetProperty("height", out var height))
                    {
                        currentHeight = height.GetInt32();
                    }

                    if (currentWidth > maxWidth && currentHeight > maxHeight)
                    {
                        maxWidth = currentWidth;
                        maxHeight = currentHeight;
                        bestImageUrl = currentUrl;
                    }

                    else if (maxWidth == 0 && maxHeight == 0 && bestImageUrl == null)
                    {
                        bestImageUrl = currentUrl;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (bestImageUrl == null)
            {
                foreach (var img in images.EnumerateArray())
                {
                    try
                    {
                        if (!img.TryGetProperty("url", out var url))
                            continue;

                        var currentUrl = url.GetString();
                        if (string.IsNullOrEmpty(currentUrl))
                            continue;

                        if (img.TryGetProperty("rel", out var rel))
                        {
                            foreach (var tag in rel.EnumerateArray())
                            {
                                var tagValue = tag.GetString();
                                if (tagValue == "full" && currentUrl.Contains(".png"))
                                {
                                    bestImageUrl = currentUrl
                                        .Replace("500", "2000")
                                        .Replace("300", "2000")
                                        .Replace("400", "2000");
                                    break;
                                }
                            }
                        }

                        if (bestImageUrl != null)
                            break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            if (bestImageUrl == null)
            {
                foreach (var img in images.EnumerateArray())
                {
                    try
                    {
                        if (img.TryGetProperty("url", out var url))
                        {
                            var currentUrl = url.GetString();
                            if (!string.IsNullOrEmpty(currentUrl) &&
                                (currentUrl.Contains(".jpg") || currentUrl.Contains(".png")))
                            {
                                bestImageUrl = currentUrl;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return bestImageUrl;
        }

        public async Task<List<NewsViewModel>> GetNbaNewsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("news?limit=50");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseNewsFromJson(json);
                }

                return new List<NewsViewModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetNbaNewsAsync: {ex.Message}");
                return new List<NewsViewModel>();
            }
        }

        private List<NewsViewModel> ParseNewsFromJson(string json)
        {
            var news = new List<NewsViewModel>();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("articles", out var articles))
                {
                    return news;
                }

                foreach (var article in articles.EnumerateArray())
                {
                    try
                    {
                        var newsItem = new NewsViewModel();

                        if (article.TryGetProperty("id", out var idElement))
                        {
                            if (idElement.ValueKind == JsonValueKind.Number)
                            {
                                newsItem.Id = idElement.GetInt32().ToString();
                            }
                            else if (idElement.ValueKind == JsonValueKind.String)
                            {
                                newsItem.Id = idElement.GetString() ?? "";
                            }
                        }
                        
                        if (article.TryGetProperty("headline", out var headline))
                        {
                            newsItem.Headline = headline.GetString() ?? "Без заголовка";
                        }

                        if (article.TryGetProperty("description", out var description))
                        {
                            newsItem.Description = description.GetString() ?? "Без описания";
                        }

                        if (article.TryGetProperty("published", out var published))
                        {
                            if (DateTime.TryParse(published.GetString(), out var pubDate))
                            {
                                newsItem.Published = pubDate;
                            }
                        }

                        newsItem.Teams = new List<string>();
                        if (article.TryGetProperty("categories", out var categories))
                        {
                            foreach (var category in categories.EnumerateArray())
                            {
                                if (category.TryGetProperty("type", out var type) &&
                                    type.GetString() == "team" &&
                                    category.TryGetProperty("description", out var teamDesc))
                                {
                                    newsItem.Teams.Add(teamDesc.GetString() ?? "");
                                }
                            }
                        }

                        if (article.TryGetProperty("links", out var links))
                        {
                            if (links.TryGetProperty("web", out var web) &&
                                web.TryGetProperty("href", out var webHref))
                            {
                                newsItem.ArticleUrl = webHref.GetString();
                            }

                            if (links.TryGetProperty("api", out var api) &&
                                api.TryGetProperty("self", out var self) &&
                                self.TryGetProperty("href", out var apiUrl))
                            {
                                newsItem.ApiUrl = apiUrl.GetString();
                            }
                        }

                        if (string.IsNullOrEmpty(newsItem.ArticleUrl) &&
                            article.TryGetProperty("gameId", out var gameId))
                        {
                            string gameIdStr = gameId.ValueKind == JsonValueKind.Number ?
                                gameId.GetInt32().ToString() : gameId.GetString() ?? "";

                            if (!string.IsNullOrEmpty(gameIdStr))
                            {
                                newsItem.ArticleUrl = $"https://www.espn.com/nba/preview?gameId={gameIdStr}";
                            }
                        }

                        newsItem.ImageUrl = GetBestImageUrl(article);

                        news.Add(newsItem);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при парсинге новости: {ex.Message}");
                        continue;
                    }
                }
            }

            return news;
        }

        public async Task<List<TeamViewModel>> GetNbaTeamsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("teams");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseTeamsFromJson(json); 
                }

                return new List<TeamViewModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetNbaTeamsAsync: {ex.Message}");
                return new List<TeamViewModel>();
            }
        }

        private List<TeamViewModel> ParseTeamsFromJson(string json)
        {
            var teams = new List<TeamViewModel>();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                var sports = root.GetProperty("sports");

                foreach (var sport in sports.EnumerateArray())
                {
                    var leagues = sport.GetProperty("leagues");
                    foreach (var league in leagues.EnumerateArray())
                    {
                        if (league.TryGetProperty("teams", out var teamsArray))
                        {
                            foreach (var teamItem in teamsArray.EnumerateArray())
                            {
                                var team = teamItem.GetProperty("team");

                                int idValue;
                                var idElement = team.GetProperty("id");

                                if (idElement.ValueKind == JsonValueKind.Number)
                                    idValue = idElement.GetInt32();
                                else
                                    idValue = int.Parse(idElement.GetString());

                                var teamModel = new TeamViewModel
                                {
                                    Id = idValue,
                                    DisplayName = team.GetProperty("displayName").GetString() ?? "Неизвестно",
                                    Location = team.GetProperty("location").GetString() ?? "Неизвестно",
                                    Color = team.TryGetProperty("color", out var color) ? color.GetString() ?? "000000" : "000000",
                                    AlternateColor = team.TryGetProperty("alternateColor", out var altColor) ? altColor.GetString() : null,
                                    LogoUrl = team.TryGetProperty("logos", out var logos) && logos.GetArrayLength() > 0
                                        ? logos[0].GetProperty("href").GetString()
                                        : null,
                                    ShortName = team.TryGetProperty("shortDisplayName", out var shortName)
                                        ? shortName.GetString()
                                        : team.GetProperty("name").GetString() ?? "Неизвестно",
                                    Abbreviation = team.TryGetProperty("abbreviation", out var abbr)
                                        ? abbr.GetString()
                                        : null
                                };

                                teams.Add(teamModel);
                            }
                        }
                    }
                }
            }

            return teams; 
        }
    }
}