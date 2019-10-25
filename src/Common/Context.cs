﻿using Discord.Commands;
using Discord.WebSocket;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using NukoBot.Database.Models;
using NukoBot.Database.Repositories;
using Discord;
using MongoDB.Driver;

namespace NukoBot.Common
{
    public class Context : SocketCommandContext
    {
        public User DbUser { get; private set; }
        public Guild DbGuild { get; private set; }
        public IGuildUser GuildUser { get; }

        private readonly IServiceProvider _serviceProvider;
        private readonly UserRepository _userRepo;
        private readonly GuildRepository _guildRepo;

        public Context(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider serviceProvider) : base(client, msg)
        {
            _serviceProvider = serviceProvider;
            _userRepo = _serviceProvider.GetService<UserRepository>();
            _guildRepo = _serviceProvider.GetService<GuildRepository>();

            GuildUser = User as IGuildUser;
        }

        public async Task InitializeAsync()
        {
            DbGuild = await _guildRepo.GetGuildAsync(Guild.Id);
            DbUser = await _userRepo.GetUserAsync(GuildUser.Id, GuildUser.GuildId);
        }
    }
}