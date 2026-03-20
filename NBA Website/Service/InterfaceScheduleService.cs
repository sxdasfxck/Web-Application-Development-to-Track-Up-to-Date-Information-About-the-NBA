using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceScheduleService
    {
        Task<ScheduleViewModel?> GetTeamScheduleAsync(string teamAbbrev);
    }
}