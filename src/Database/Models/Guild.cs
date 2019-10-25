﻿using MongoDB.Bson;

namespace NukoBot.Database.Models
{
    public partial class Guild : Model
    {
        public Guild(ulong guildId)
        {
            GuildId = guildId;
        }

        public ulong GuildId { get; set; }

        public BsonDocument ModRoles { get; set; } = new BsonDocument();

        public ulong MutedRoleId { get; set; }

        public ulong ModLogChannelId { get; set; }

        public ulong WelcomeChannelId { get; set; }

        public string WelcomeMessage { get; set; } = string.Empty;

        public int CaseNumber { get; set; } = 1;
    }
}