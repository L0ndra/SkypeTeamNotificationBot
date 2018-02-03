using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using MongoDB.Bson;
using MongoDB.Driver;
using Remotion.Linq.Clauses;
using SkypeTeamNotificationBot.DataModels;

namespace SkypeTeamNotificationBot.DataAccess
{
    public class UsersDal
    {
        private MongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<UserModel> _colection;

        public UsersDal(string conectionString)
        {
            _client = new MongoClient(conectionString);
            _db = _client.GetDatabase("DevelopexBotDb");
            _colection = _db.GetCollection<UserModel>("Users");
        }

        public async Task<IEnumerable<UserModel>> GetUsersAsync()
        {
            return await _colection.Find(new BsonDocumentFilterDefinition<DataModels.UserModel>(new BsonDocument())).ToListAsync();
        }

        public async Task AddNewUserAsync(UserModel user)
        {
            await _colection.InsertOneAsync(user);
        }

        public async Task<UserModel> GetUserByNameAsync(string name)
        {
            return await _colection.Find(x => x.Name == name).FirstAsync();
        }


    }
}