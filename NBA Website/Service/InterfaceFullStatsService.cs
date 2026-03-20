using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceFullStatsService
    {
        Task<FullStatsViewModel?> GetFullStatsAsync(string sortBy = "PTS", string type = "main", int page = 1);
    }
}