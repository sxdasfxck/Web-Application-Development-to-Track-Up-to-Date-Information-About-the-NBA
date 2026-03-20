using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceTeamDetailService
    {
        Task<TeamDetailViewModel?> GetTeamDetailAsync(string abbreviation);
    }
}