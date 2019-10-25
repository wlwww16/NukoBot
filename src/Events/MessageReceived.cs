﻿using Discord.WebSocket;
using System.Threading.Tasks;
using NukoBot.Common;
using NukoBot.Services;
using Discord;
using Discord.Commands;
using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NukoBot.Database.Repositories;

namespace NukoBot.Events
{
    public sealed class MessageReceived
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Text _text;
        private readonly GuildRepository _guildRepo;

        public MessageReceived(DiscordSocketClient client, CommandService commandService, IServiceProvider serviceProvider)
        {
            _client = client;
            _commandService = commandService;
            _serviceProvider = serviceProvider;
            _text = _serviceProvider.GetRequiredService<Text>();
            _guildRepo = _serviceProvider.GetRequiredService<GuildRepository>();

            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var argPos = 0;

            if (!message.HasStringPrefix(Configuration.Prefix, ref argPos)) return;

            //var context = new SocketCommandContext(_client, message);
            var context = new Context(_client, message, _serviceProvider);

            await context.InitializeAsync();

            //var dbGuild = await _guildRepo.GetGuildAsync(context.Guild.Id);

            //Console.WriteLine("dbGuild.GuildId: " + dbGuild.GuildId);
            //Console.WriteLine("context.Guild.Id: " + context.Guild.Id);

            var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

            if (!result.IsSuccess)
            {
                if (result.Error == CommandError.UnknownCommand)
                {
                    return;
                }

                await _text.ReplyErrorAsync(message.Author, context.Channel, $"I'm sorry but an error occurred whilst executing that command:\n\n```{result.ErrorReason}```");
            }
        }
    }
}
