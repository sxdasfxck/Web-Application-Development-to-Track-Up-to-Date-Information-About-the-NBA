using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceLeadersService
    {
        Task<LeadersViewModel?> GetLeadersAsync();
    }
}