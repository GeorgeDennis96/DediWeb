using DediBotWeb.Common.Models;

namespace DediBotWeb.Services
{
    public interface IRankingService
    {
        public Task<List<PlayerModel>> LoadTopPlayers(int amount);
    }
}