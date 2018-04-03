using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using SkypeTeamNotificationBot.DataModels;
using SkypeTeamNotificationBot.Utils;

namespace SkypeTeamNotificationBot.DataAccess
{
    public static class UsersDal
    {
        private static MongoClient _client;
        private static IMongoDatabase _db;
        private static IMongoCollection<UserModel> _colection;
        private static ILogger _logger;

        public static void Initialize(string conectionString)
        {
            _client = new MongoClient(conectionString);
            _db = _client.GetDatabase("DevelopexBotDb");
            _colection = _db.GetCollection<UserModel>("Users");
            _logger = LoggerProxy.Logger(nameof(UsersDal));
        }

        public static async Task<IEnumerable<UserModel>> GetUsersAsync()
        {
            try
            {
                return await _colection.Find(new BsonDocumentFilterDefinition<UserModel>(new BsonDocument()))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get all users from db with exception: {0}", ex);
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
                _logger.LogError("Failed to inser new user : '{0}' with exception '{1}'",
                    JsonConvert.SerializeObject(user), ex);
                throw;
            }
        }

        public static async Task UpdateUserAsync(UserModel user)
        {
            try
            {
                await _colection.ReplaceOneAsync(x => x.Id == user.Id, user);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update user : '{0}' with exception '{1}'",
                    JsonConvert.SerializeObject(user), ex);
            }
        }

        public static async Task<UserModel> GetUserByIdAsync(string id)
        {
            try
            {
                return await _colection.Find(x => x.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get user with id '{0}' with exception '{1}'", id, ex);
                throw;
            }
        }

        public static async Task<IEnumerable<UserModel>> GetUsersWithSpecificConditionAsync(
            Expression<Func<UserModel, bool>> condition)
        {
            try
            {
                return await _colection.Find(condition).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get user by condition '{0}' with exception '{1}'",
                    JsonConvert.SerializeObject(condition), ex);
                throw;
            }
        }


    }
}