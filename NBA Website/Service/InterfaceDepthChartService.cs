using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceDepthChartService
    {
        Task<DepthChartViewModel?> GetTeamDepthChartAsync(string teamAbbrev);
    }
}