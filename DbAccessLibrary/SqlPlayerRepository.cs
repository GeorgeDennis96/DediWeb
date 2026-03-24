using Dapper;
using DediBotWeb.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DbAccessLibrary
{
    public class SqlPlayerRepository : IPlayerRepo
    {
        private readonly IConfiguration _config;
        private readonly string _connectionStringName = "Default";

        public SqlPlayerRepository(IConfiguration config)
        {
            this._config = config;
        }

        public Task<List<PlayerModel>> GetPlayersAsync()
        {
            string sql = "SELECT * FROM dbo.Player";
            return LoadData<PlayerModel, dynamic>(sql, new { });
        }

        public Task AddPlayerAsync (PlayerModel player)
        {
            string sql = @"
            INSERT INTO dbo.Player (InternalId, DiscordId, Username, TotalGamesPlayed, Wins, Losses, WinRate, Balance)
            SELECT @InternalId, @DiscordId, @Username, @TotalGamesPlayed, @Wins, @Losses, @WinRate, @Balance
            WHERE NOT EXISTS (
            SELECT 1 FROM dbo.Player WHERE DiscordId = @DiscordId);";

            return SaveData(sql, player);
        }

        public async Task<PlayerModel> GetPlayerByInternalIdAsync(Guid internalId)
        {
            string sql = "SELECT * FROM dbo.Player WHERE InternalId = @InternalId";

            var result = await LoadData<PlayerModel, dynamic>(sql, new { InternalId = internalId });
            return result.FirstOrDefault();
        }

        public async Task<PlayerModel> GetPlayerByDiscordIdAsync(decimal discordId)
        {
            string sql = "SELECT * FROM dbo.Player WHERE DiscordId = @DiscordId";
            var result = await LoadData<PlayerModel, dynamic>(sql, new { DiscordId = discordId });
            return result.FirstOrDefault();
        }

        public Task UpdatePlayerAsync(PlayerModel player)
        {
            string sql = @"UPDATE dbo.Player 
                           SET TotalGamesPlayed = @TotalGamesPlayed, Wins = @Wins, Losses = @Losses, Balance = @Balance, DailyClaimedAt = @DailyClaimedAt
                           WHERE InternalId = @InternalId";
            return SaveData(sql, player);
        }

        public async Task<List<PlayerModel>> GetTopPlayersAsync(int amount = 100)
        {
            string sql = $"SELECT TOP {amount} * FROM dbo.Player ORDER BY Balance DESC";
            var result = await LoadData<PlayerModel, dynamic>(sql, new { });
            return result;
        }

        #region Helper Methods

        private string GetConnectionStringOrThrow()
        {
            var connectionString = _config.GetConnectionString(_connectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{_connectionStringName}' not found. Ensure it exists in configuration (e.g., appsettings.json or user secrets).");
            }

            return connectionString;
        }

        public async Task<List<T>> LoadData<T, U>(string sql, U parameters)
        {
            var connectionString = GetConnectionStringOrThrow();
            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                var data = await connection.QueryAsync<T>(sql, parameters);
                return data.ToList();
            }
        }

        public async Task SaveData<T>(string sql, T parameters)
        {
            var connectionString = GetConnectionStringOrThrow();
            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, parameters);
            }
        }

        #endregion
    }
}
