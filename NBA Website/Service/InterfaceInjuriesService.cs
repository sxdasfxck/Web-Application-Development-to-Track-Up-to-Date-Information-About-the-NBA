using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceInjuriesService
    {
        Task<InjuriesViewModel?> GetTeamInjuriesAsync(string teamAbbrev);
    }
}