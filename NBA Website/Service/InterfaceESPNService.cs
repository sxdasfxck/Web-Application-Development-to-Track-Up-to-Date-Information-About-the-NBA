using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfaceESPNService
    {
        Task<List<TeamViewModel>> GetNbaTeamsAsync();
        Task<List<NewsViewModel>> GetNbaNewsAsync();
    }
}