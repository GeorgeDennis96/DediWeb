using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace DbAccessLibrary
{
    public class DbDataAccess : IDbDataAccess
    {
        private readonly IConfiguration _config;
        private readonly string _connectionStringName = "Default";

        public DbDataAccess(IConfiguration configuration)
        {
            _config = configuration;
        }

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

        public async Task<int> SaveData<T>(string sql, T parameters)
        {
            var connectionString = GetConnectionStringOrThrow();
            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                return await connection.ExecuteAsync(sql, parameters);
            }
        }
    }
}

