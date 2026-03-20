using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceCalendarService
    {
        Task<CalendarViewModel?> GetCalendarAsync();
    }
}