using DediBotWeb.Common.Models;

namespace DbAccessLibrary
{
    public interface IPlayerData
    {
        Task<List<PlayerModel>> GetPlayers();
        Task<int> InsertNewPlayer(PlayerModel player);
        Task<PlayerModel> GetPlayerByInternalId(Guid internalId);
        Task<PlayerModel> GetPlayerByDiscordId(decimal discordId);
        Task UpdatePlayer(PlayerModel player);
        Task<List<PlayerModel>> GetTopPlayers(int amount);
    }
}