﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using NukoBot.Common;
using NukoBot.Database.Repositories;
using NukoBot.Common.Preconditions.Command;
using NukoBot.Services;
using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Linq;

namespace NukoBot.Modules
{
    [Name("Administrator")]
    [Summary("Commands only allowed to be used by users with a role with a permission level of at least 2.")]
    [RequireAdministrator]
    public sealed class Administrator : ModuleBase<Context>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Text _text;
        private readonly GuildRepository _guildRepository;
        private readonly UserRepository _userRepository;
        private readonly ModerationService _moderationService;

        public Administrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _text = _serviceProvider.GetRequiredService<Text>();
            _guildRepository = _serviceProvider.GetRequiredService<GuildRepository>();
            _userRepository = _serviceProvider.GetRequiredService<UserRepository>();
            _moderationService = _serviceProvider.GetRequiredService<ModerationService>();
        }

        [Command("SetScreenshotChannel")]
        [Alias("removescreenshotchannel")]
        [Summary("Set the channel for screenshots to be submitted to.")]
        public async Task SetScreenshotChannel([Summary("The channel you want to set as the screenshot submitting channel.")] [Remainder] ITextChannel screenshotChannel = null)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.ScreenshotChannelId = screenshotChannel.Id);

            if (screenshotChannel != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the screenshot channel to {screenshotChannel.Mention}.");
                return;
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the screenshot channel.");
        }

        [Command("SetMutedRole")]
        [Alias("setmuterole", "removemutedrole", "removemuterole")]
        [Summary("Set the role to be given to those who are muted.")]
        public async Task SetMutedRole([Summary("The role you want to set as the muted role.")] [Remainder] IRole mutedRole = null)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.MutedRoleId = mutedRole.Id);

            if (mutedRole != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the muted role to {mutedRole.Mention}.");
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the muted role.");
        }

        [Command("SetModLogChannel")]
        [Alias("setlogchannel", "setmodlog", "removemodlogchannel", "removelogchannel")]
        [Summary("Set the channel for all moderator actions to be logged to.")]
        public async Task SetModLogChannel([Summary("The channel you wish to set as the mod log channel.")] [Remainder] ITextChannel modLogChannel = null)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.ModLogChannelId = modLogChannel.Id);

            if (modLogChannel != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the mod log channel to {modLogChannel.Mention}.");
                return;
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the mod log channel.");
        }

        [Command("SetWelcomeMessage")]
        [Alias("setwelcomemsg", "deletewelcomemessage", "deletewelcomemsg", "removewelcomemessage", "removewelcomemsg")]
        [Summary("Set the message to sent when a user joins this server.")]
        public async Task SetWelcomeChannel([Summary("The welcome message to be DMed to new users.")] [Remainder] string welcomeMessage = null)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.WelcomeMessage = welcomeMessage);

            if (welcomeMessage != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the welcome message to **{welcomeMessage}**");
                return;
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the welcome message.");
        }

        [Command("SetNewUserRole")]
        [Alias("setdefaultrole")]
        [Summary("Set the role to be given to all new users in the server.")]
        public async Task SetNewUserRole([Summary("The role to be given to new users.")] [Remainder] IRole newUserRole = null)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.NewUserRole = newUserRole.Id);

            if (newUserRole != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the new user role to {newUserRole.Mention}.");
                return;
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the new user role.");
        }

        [Command("Award")]
        [Alias("awardpoints", "givepoints")]
        [Summary("Add points to a user which will also increase the glboal point counter.")]
        public async Task Award([Summary("The round the user died on.")] int round, [Summary("The difficulty of the map the user played on.")] int difficulty, [Summary("The user you wish to give the points to.")] IGuildUser user, [Summary("Whether or not the game was played with a friend in the server.")] [Remainder] bool playedWithOther = false)
        {
            if (difficulty < 1 || difficulty > 3)
            {
                await _text.ReplyErrorAsync(Context.User, Context.Channel, $"The difficulty **{difficulty}** was not found. Please use either 1, 2 or 3 corresponding to easy, normal or hard.");
                return;
            }

            var dbUser = await _userRepository.GetUserAsync(user.Id, Context.Guild.Id);

            if (dbUser == null)
            {
                await _text.ReplyErrorAsync(Context.User, Context.Channel, $"that user was found.");
                return;
            }

            double multiplier = 1;
            
            switch (difficulty)
            {
                case 1:
                    multiplier = 0.75;
                    break;
                case 2:
                    multiplier = 1.0;
                    break;
                case 3:
                    multiplier = 1.25;
                    break;
            }

            double points = (double)Math.Ceiling(round * multiplier);
            
            if (playedWithOther == true)
            {
                points = Math.Ceiling(points + (points * 10 / 100));
            }

            await _userRepository.ModifyAsync(dbUser, x => x.Points += (int)points);

            var userDm = await user.GetOrCreateDMChannelAsync();

            await _text.SendAsync(userDm, $"**{Context.User.Mention}** has awarded you **{points}** points in **{Context.Guild.Name}**.");

            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.Points += (int)points);

            await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully added **{points}** points to {user.Mention}.");

            var thirdPlaceDbUser = (await _userRepository.AllAsync(x => x.GuildId == Context.Guild.Id)).OrderByDescending(x => x.Points).ElementAtOrDefault(2);

            if (thirdPlaceDbUser == null)
            {
                await user.AddRoleAsync(Context.Guild.GetRole(Context.DbGuild.TopThreeRole));
                return;
            }

            if (dbUser.Points > thirdPlaceDbUser.Points)
            {
                var thirdPlaceGuildUser = Context.Guild.GetUser(thirdPlaceDbUser.UserId);

                await user.AddRoleAsync(Context.Guild.GetRole(Context.DbGuild.TopThreeRole));

                await thirdPlaceGuildUser.RemoveRoleAsync(Context.Guild.GetRole(Context.DbGuild.TopThreeRole));
            }
        }

        [Command("Deduct")]
        [Alias("deductpoints", "removepoints")]
        [Summary("Remove points from a user which will also decrease the global point counter.")]
        public async Task Deduct([Summary("The amount of points to be taken away.")] int amountOfPoints, [Summary("The user you want to take ponts away from.")] IGuildUser user = null, [Remainder] string reason = null)
        {
            if (user == null)
            {
                if (Context.DbGuild.Points - amountOfPoints < 0)
                {
                    await _guildRepository.ModifyAsync(Context.DbGuild, x => x.Points = 0);
                }
                else
                {
                    await _guildRepository.ModifyAsync(Context.DbGuild, x => x.Points -= amountOfPoints);
                }

                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully removed **{amountOfPoints}** from this guild's total.");
            }
            else
            {
                var dbUser = await _userRepository.GetUserAsync(user.Id, Context.Guild.Id);

                if (dbUser == null)
                {
                    await _text.ReplyErrorAsync(Context.User, Context.Channel, $"that user was found.");
                    return;
                }

                if (Context.DbUser.Points - amountOfPoints < 0)
                {
                    await _userRepository.ModifyAsync(dbUser, x => x.Points = 0);
                }
                else
                {
                    await _userRepository.ModifyAsync(dbUser, x => x.Points -= amountOfPoints);
                }

                if (Context.DbGuild.Points - amountOfPoints < 0)
                {
                    await _guildRepository.ModifyAsync(Context.DbGuild, x => x.Points = 0);
                }
                else
                {
                    await _guildRepository.ModifyAsync(Context.DbGuild, x => x.Points -= amountOfPoints);
                }

                var userDm = await user.GetOrCreateDMChannelAsync();
                var message = $"**{Context.User.Mention}** has deducted **{amountOfPoints}** points from you in **{Context.Guild.Name}**";

                message += reason != null ? $" for **{reason}**." : ".";

                await _text.SendAsync(userDm, message);

                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully removed **{amountOfPoints}** from {user.Mention}.");
            }
        }

        [Command("Ban")]
        [Alias("banish")]
        [Summary("Ban any user from being in the server.")]
        public async Task Ban([Summary("The user to ban.")] IGuildUser userToBan, [Summary("The reason for banning the user.")] [Remainder] string reason = null)
        {
            if (_moderationService.GetPermissionLevel(Context.DbGuild, userToBan) > 0)
            {
                await _text.ReplyErrorAsync(Context.User, Context.Channel, $"{userToBan.Mention} is a moderator and thus cannot be banned.");
                return;
            }

            string message = $"{Context.User.Mention} has banned you from **{Context.Guild.Name}**";

            if (reason.Length > 0)
            {
                message += $" for **{reason}**";
            }

            await _moderationService.InformUserAsync((SocketUser) userToBan, message + ".");

            await userToBan.BanAsync(0, reason);

            await _moderationService.ModLogAsync(Context.DbGuild, Context.Guild, "Ban", Configuration.KickColor, reason, Context.User as IGuildUser, userToBan);
        }

        [Command("SetTop3Role")]
        [Alias("settoprole", "setlbrole", "setleaderboardrole", "settop3")]
        [Summary("Set the role to be given to the 3 users with the most points.")]
        public async Task SetTopThreeRole([Summary("The role to be given.")] [Remainder] IRole topThreeRole)
        {
            await _guildRepository.ModifyAsync(Context.DbGuild, x => x.TopThreeRole = topThreeRole.Id);

            if (topThreeRole != null)
            {
                await _text.ReplyAsync(Context.User, Context.Channel, $"you have successfully set the top 3 role to {topThreeRole.Mention}.");
                return;
            }

            await _text.ReplyAsync(Context.User, Context.Channel, "you have successfully removed the top 3 role.");
        }
    }
}
