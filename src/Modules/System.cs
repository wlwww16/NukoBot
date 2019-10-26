﻿using Discord.Commands;
using NukoBot.Common;
using NukoBot.Services;
using NukoBot.Extensions;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace NukoBot.Modules
{
    [Name("System")]
    [Summary("The most basic commands available to all users.")]
    public sealed class System : ModuleBase
    {
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Text _text;

        public System(CommandService commandService, IServiceProvider serviceProvider)
        {
            _commandService = commandService;
            _serviceProvider = serviceProvider;
            _text = _serviceProvider.GetRequiredService<Text>();
        }

        [Command("help")]
        [Alias("info")]
        [Summary("View the basic info regarding this bot.")]
        public async Task Help()
        {
            var userDm = await Context.User.GetOrCreateDMChannelAsync();

            await _text.SendAsync(userDm, Configuration.HelpMessage);

            if (Context.Channel != userDm)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, "please check your DMs.");
            }
        }

        [Command("commands")]
        [Alias("modules", "module", "command")]
        [Summary("View all modules or all commands in a specific module.")]
        public async Task Commands([Summary("A specific command or module you wish to learn about.")][Remainder] string commandOrModule = null)
        {
            var userDm = await Context.User.GetOrCreateDMChannelAsync();

            if (commandOrModule != null)
            {
                commandOrModule = commandOrModule.ToLower();

                var foundCommand = _commandService.Commands.SingleOrDefault(x => x.Name.ToLower() == commandOrModule);
                var foundModule = _commandService.Modules.SingleOrDefault(x => x.Name.ToLower() == commandOrModule);
                var commands = string.Empty;
                var message = $"`{commandOrModule}` could refer to:\n\n";

                if (foundModule != null)
                {
                    foreach (var command in foundModule.Commands)
                    {
                        commands += $"{StringExtension.FirstCharToUpper(command.Name)}: *{command.Summary}*\n";
                    }

                    message += $"**Commands in the {foundModule.Name} module**:\n{commands}\n";
                }

                message += foundCommand != null ? $"\n\n**Miscellaneous commands:**\n{StringExtension.FirstCharToUpper(foundCommand.Name)}: *{foundCommand.Summary}*" : null;

                await _text.ReplyAsync(Context.User, userDm, message, "Command information");

                if (Context.Channel != userDm) await _text.ReplyAsync(Context.User, Context.Channel, "please check your DMs.");

                return;
            }

            var modules = string.Empty;

            foreach (var module in _commandService.Modules)
            {
                modules += $"__{module.Name}__: *{module.Summary}*\n";
            }

            await _text.SendAsync(userDm, $"**Modules**:\n{modules.Remove(modules.Length - 2)}\n\nTo view the commands in any given module, please say `{Configuration.Prefix}module <moduleName>`.", "Module information");

            if (Context.Channel != userDm) await _text.ReplyAsync(Context.User, Context.Channel, "please check your DMs.");
        }

        [Command("support")]
        [Summary("Displays the invitation link to the support server.")]
        public Task Support()
        {
            return _text.ReplyAsync(Context.User, Context.Channel, "For bot support, selfhosting support, feature requests or just to chat, join the support server [here](" + Configuration.SupportServerLink + ").");
        }

        [Command("echo")]
        [Alias("say", "embed")]
        public Task Echo([Summary("The text you want the bot to embed.")][Remainder] string message)
        {
            return _text.SendAsync(Context.Channel, message);
        }
    }
}