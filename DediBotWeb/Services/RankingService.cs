using DbAccessLibrary;
using DediBotWeb.Common.Models;

namespace DediBotWeb.Services
{

    public class RankingService : IRankingService
    {
        private readonly IPlayerData PlayerData;
        public List<PlayerModel> Top1000Players { get; private set; } = new List<PlayerModel>();

        public RankingService(IPlayerData playerData)
        {
            this.PlayerData = playerData;
        }
        public async Task<List<PlayerModel>> LoadTopPlayers(int amount)
        {
            return await PlayerData.GetTopPlayers(amount);
        }
    }
}
