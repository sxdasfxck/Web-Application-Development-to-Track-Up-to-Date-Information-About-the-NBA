using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceStandingsService
    {
        Task<List<StandingViewModel>> GetStandingsAsync();
    }
}