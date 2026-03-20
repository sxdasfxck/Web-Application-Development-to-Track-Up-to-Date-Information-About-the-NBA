using NBA_Website.Models;

namespace NBA_Website.Services
{
    public interface InterfacePlayersPageService
    {
        Task<PlayersPageViewModel?> GetPlayersListAsync(int page = 1);
        Task<PlayersPageViewModel?> GetPlayerDetailsAsync(string playerId);
    }
}