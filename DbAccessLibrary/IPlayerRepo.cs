using DediBotWeb.Common.Models;

namespace DbAccessLibrary
{
    public interface IPlayerRepo
    {
        Task<List<PlayerModel>> GetPlayersAsync();
        Task AddPlayerAsync(PlayerModel player);
        Task<PlayerModel> GetPlayerByInternalIdAsync(Guid internalId);
        Task<PlayerModel> GetPlayerByDiscordIdAsync(decimal discordId);
        Task UpdatePlayerAsync(PlayerModel player);
        Task<List<PlayerModel>> GetTopPlayersAsync(int amount);
    }
}