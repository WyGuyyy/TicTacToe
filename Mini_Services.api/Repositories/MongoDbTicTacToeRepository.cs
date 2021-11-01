using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mini_Services.api.Repositories
{
    public class MongoDbTicTacToeRepository : ITicTacToeRepository
    {

        private const string databaseName = "mini_services";
        private const string collectionName = "tictactoe";
        private readonly FilterDefinitionBuilder<TicTacToe> filterBuilder = Builders<TicTacToe>.Filter;
        private readonly IMongoCollection<TicTacToe> ticTacToeCollection;

        public MongoDbTicTacToeRepository(IMongoClient mongoClient)
        {
            IMongoDatabase database = mongoClient.GetDatabase(databaseName);
            ticTacToeCollection = database.GetCollection<TicTacToe>(collectionName);
        }

        public async Task CreateSessionAsync(TicTacToe ticTacToe)
        {
            await ticTacToeCollection.InsertOneAsync(ticTacToe);
        }

        public async Task DeleteSessionAsync(Guid sessionId)
        {
            var filter = filterBuilder.Eq(session => session.Id, sessionId);
            await ticTacToeCollection.DeleteOneAsync(filter);
        }

        public async Task<TicTacToe> GetSessionAsync(Guid sessionId)
        {
            var filter = filterBuilder.Eq(session => session.Id, sessionId);
            return await ticTacToeCollection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<TicTacToe>> GetSessionsAsync()
        {
            return await ticTacToeCollection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task UpdateSessionAsync(TicTacToe ticTacToe)
        {
            var filter = filterBuilder.Eq(existingSession => existingSession.Id, ticTacToe.Id);
            await ticTacToeCollection.ReplaceOneAsync(filter, ticTacToe);
        }
    }
}