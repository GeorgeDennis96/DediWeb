using DbAccessLibrary;
using DediBotWeb.Common.Models;

namespace DediBotWeb.Services
{

    public class RankingService : IRankingService
    {
        private readonly IPlayerRepo PlayerData;
        public List<PlayerModel> TopPlayers { get; private set; } = new List<PlayerModel>();

        public RankingService(IPlayerRepo playerData)
        {
            this.PlayerData = playerData;
        }
        public async Task<List<PlayerModel>> LoadTopPlayers(int amount)
        {
            return await PlayerData.GetTopPlayersAsync(amount);
        }
    }
}
