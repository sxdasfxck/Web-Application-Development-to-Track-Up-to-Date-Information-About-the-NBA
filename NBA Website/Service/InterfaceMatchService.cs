using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceMatchService
    {
        Task<MatchViewModel?> GetMatchAsync(string eventId);
    }
}