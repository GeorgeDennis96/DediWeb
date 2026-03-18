using DediBotWeb.Common.Models;

namespace DbAccessLibrary
{
    public class PlayerData : IPlayerData
    {
        private readonly IDbDataAccess _db;
        public PlayerData(IDbDataAccess dbDataAccess)
        {
            _db = dbDataAccess;
        }

        public Task<List<PlayerModel>> GetPlayers()
        {
            string sql = "SELECT * FROM dbo.Player";
            return _db.LoadData<PlayerModel, dynamic>(sql, new { });
        }

        public Task<int> InsertNewPlayer(PlayerModel player)
        {
            string sql = @"
            INSERT INTO dbo.Player (InternalId, DiscordId, Username, TotalGamesPlayed, Wins, Losses, WinRate, Balance)
            SELECT @InternalId, @DiscordId, @Username, @TotalGamesPlayed, @Wins, @Losses, @WinRate, @Balance
            WHERE NOT EXISTS (
            SELECT 1 FROM dbo.Player WHERE DiscordId = @DiscordId
        );";

            return _db.SaveData(sql, player);
        }

        public async Task<PlayerModel> GetPlayerByInternalId(Guid internalId)
        {
            string sql = "SELECT * FROM dbo.Player WHERE InternalId = @InternalId";

            var result = await _db.LoadData<PlayerModel, dynamic>(sql, new { InternalId = internalId });
            return result.FirstOrDefault();
        }

        public async Task<PlayerModel> GetPlayerByDiscordId(decimal discordId)
        {
            string sql = "SELECT * FROM dbo.Player WHERE DiscordId = @DiscordId";
            var result = await _db.LoadData<PlayerModel, dynamic>(sql, new { DiscordId = discordId });
            return result.FirstOrDefault();
        }

        public Task UpdatePlayer(PlayerModel player)
        {
            string sql = @"UPDATE dbo.Player 
                           SET TotalGamesPlayed = @TotalGamesPlayed, Wins = @Wins, Losses = @Losses, Balance = @Balance, DailyClaimedAt = @DailyClaimedAt
                           WHERE InternalId = @InternalId";
            return _db.SaveData(sql, player);
        }

        public async Task<List<PlayerModel>> GetTopPlayers(int amount = 100)
        {
            string sql = $"SELECT TOP {amount} * FROM dbo.Player ORDER BY Balance DESC";
            return await _db.LoadData<PlayerModel, dynamic>(sql, new { });
        }
    }
}
