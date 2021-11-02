using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mini_Services.api.Repositories
{
    public class MongoDbPlayerRepository : IPlayerRepository
    {
        private const string databaseName = "mini_services";
        private const string collectionName = "player";
        private readonly IMongoCollection<Player> playerCollection;
        private readonly FilterDefinitionBuilder<Player> filterBuilder = Builders<Player>.Filter;

        public MongoDbPlayerRepository(IMongoClient mongoClient)
        {
            IMongoDatabase database = mongoClient.GetDatabase(databaseName);
            playerCollection = database.GetCollection<Player>(collectionName);
        }

        public async Task CreateAccountAsync(Player player)
        {
            await playerCollection.InsertOneAsync(player);
        }

        public async Task DeletePlayerAsync(Guid Id)
        {
            var filter = filterBuilder.Eq(existingPlayer => existingPlayer.Id, Id);
            await playerCollection.DeleteOneAsync(filter);
        }

        public async Task<Player> GetAccountAsync(Guid Id)
        {
            var filter = filterBuilder.Eq(player => player.Id, Id);
            return await playerCollection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<Player>> GetAccountsAsync()
        {
            return await playerCollection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task UpdatePlayerAsync(Player player)
        {
            var filter = filterBuilder.Eq(existingPlayer => existingPlayer.Id, player.Id);
            await playerCollection.ReplaceOneAsync(filter, player);
        }
    }
}