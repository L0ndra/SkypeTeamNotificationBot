﻿using Microsoft.Bot.Connector;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SkypeTeamNotificationBot.DataModels
{
    public enum Role
    {
        User = 0,
        Admin = 1
    }

    public class UserModel
    {
        public ObjectId Id { get; set; }
        
        [BsonElement("Name")]
        public string Name { get; set; }
        
        [BsonElement("Role")]
        public Role Role { get; set; }
        
        [BsonElement("Activity")]
        public Activity Activity { get; set; }
    }
}