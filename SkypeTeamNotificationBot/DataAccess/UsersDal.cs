using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using MongoDB.Bson;
using MongoDB.Driver;
using Remotion.Linq.Clauses;
using SkypeTeamNotificationBot.DataModels;

namespace SkypeTeamNotificationBot.DataAccess
{
    public static class UsersDal
    {
        private static MongoClient _client;
        private static IMongoDatabase _db;
        private static IMongoCollection<UserModel> _colection;

        public static void Initialize(string conectionString)
        {
            _client = new MongoClient(conectionString);
            _db = _client.GetDatabase("DevelopexBotDb");
            _colection = _db.GetCollection<UserModel>("Users");
        }

        public static async Task<IEnumerable<UserModel>> GetUsersAsync()
        {
            try
            {
                return await _colection.Find(new BsonDocumentFilterDefinition<DataModels.UserModel>(new BsonDocument()))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                //todo: add logs and some handlers in caller methods
                throw;
            }
        }

        public static async Task<UserModel> InsertUserAsync(UserModel user)
        {
            try
            {
                var users = await _colection.CountAsync(x => x.Role == Role.Admin);
                if (users == 0)
                {
                    user.Role = Role.Admin;
                }
                await _colection.InsertOneAsync(user);
                return user;
            }
            catch(Exception ex)
            {
                //todo: add logs and some handlers in caller methods
                throw;
            }
        }

        public static async Task UpdateUserAsync(UserModel user)
        {
            await _colection.ReplaceOneAsync(x => x.Id == user.Id, user);
        }

        public static async Task<UserModel> GetUserByIdAsync(string id)
        {
            try
            {
                return await _colection.Find(x => x.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                //todo: add logs and some handlers in caller methods
                throw;
            }
        }

        public static async Task<IEnumerable<UserModel>> GetUsersWithSpecificConditionAsync(Expression<Func<UserModel, bool>> condition)
        {
            try
            {
                return await _colection.Find(condition).ToListAsync();
            }
            catch
            {
                //todo: add logs and some handlers in caller methods
                throw;
            }
        }


    }
}