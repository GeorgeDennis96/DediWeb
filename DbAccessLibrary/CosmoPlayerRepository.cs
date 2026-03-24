using Azure.Core;
using Azure.Identity;
using DbAccessLibrary;
using DediBotWeb.Common.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

public class CosmosPlayerRepository : IPlayerRepo
{
    private readonly string ContainerName = "Container1";
    private readonly string DatabaseName = "Players";
    private readonly Container _container;
    private CosmosClient _client;

    public CosmosPlayerRepository(IConfiguration configuration)
    {
        _client = new(connectionString: configuration["CosmoDb:ConnectionString"]);
        Database database = _client.GetDatabase(DatabaseName);
        _container = database.GetContainer(ContainerName);
    }

    public async Task<List<PlayerModel>> GetPlayersAsync()
    {
        try
        {
            var playerList = new List<PlayerModel>();

            // Query multiple items from container
            using FeedIterator<PlayerModel> feed = _container.GetItemQueryIterator<PlayerModel>(
                queryText: $"SELECT * FROM {ContainerName}"
            );

            while (feed.HasMoreResults)
            {
                FeedResponse<PlayerModel> response = await feed.ReadNextAsync();
                foreach (var item in response)
                {
                    playerList.Add(item);
                }
            }

            return playerList;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task AddPlayerAsync(PlayerModel player)
    {
        await _container.CreateItemAsync(player, new PartitionKey(player.InternalId.ToString()));
    }

    public async Task<PlayerModel> GetPlayerByInternalIdAsync(Guid internalId)
    {
        var queryDef = new QueryDefinition(
            "SELECT * FROM c WHERE c.InternalId = @internalId"
        )
        .WithParameter("@internalId", internalId.ToString().ToUpperInvariant());

        var iterator = _container.GetItemQueryIterator<PlayerModel>(queryDef);

        var results = new List<PlayerModel>();

        var response = await iterator.ReadNextAsync();
        results.AddRange(response);

        return results.FirstOrDefault();
    }

    public async Task<PlayerModel> GetPlayerByDiscordIdAsync(decimal discordId)
    {
        var queryDef = new QueryDefinition(
            "SELECT * FROM c WHERE c.DiscordId = @discordId"
        )
        .WithParameter("@discordId", discordId.ToString());

        var iterator = _container.GetItemQueryIterator<PlayerModel>(queryDef);

        var results = new List<PlayerModel>();

        var response = await iterator.ReadNextAsync();
        results.AddRange(response);

        return results.FirstOrDefault();
    }

    public Task UpdatePlayerAsync(PlayerModel player)
    {
        throw new NotImplementedException();
    }

    public Task<List<PlayerModel>> GetTopPlayersAsync(int amount)
    {
        throw new NotImplementedException();
    }
}