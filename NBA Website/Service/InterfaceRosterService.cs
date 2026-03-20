using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceRosterService
    {
        Task<RosterViewModel?> GetRosterInfoAsync(string teamAbbrev);
    }
}