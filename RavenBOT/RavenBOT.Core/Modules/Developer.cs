using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Common;

namespace RavenBOT.Core.Modules
{
    [RavenRequireOwner]
    [Group("Developer")]
    public class Developer : ModuleBase<ShardedCommandContext>
    {
        public IServiceProvider Provider { get; }

        public Developer(IServiceProvider provider)
        {
            Provider = provider;
        }

        [Command("EditHelpPreconditionSkips")]
        public async Task EditHelpPreconditionSkipsAsync(string skip)
        {
            var settings = Provider.GetRequiredService<DeveloperSettings>().GetDeveloperSettings();
            if (settings.SkippableHelpPreconditions.Contains(skip))
            {
                await ReplyAsync($"Removed {skip}");
                settings.SkippableHelpPreconditions.Remove(skip);
            }
            else
            {
                await ReplyAsync($"Added {skip}");
                settings.SkippableHelpPreconditions.Add(skip);
            }

            Provider.GetRequiredService<DeveloperSettings>().SetDeveloperSettings(settings);

            await ReplyAsync("Settings:\n" +
                $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("ClearHelpPreconditionSkips")]
        public async Task ClearHelpPreconditionSkipsAsync()
        {
            var settings = Provider.GetRequiredService<DeveloperSettings>().GetDeveloperSettings();
            settings.SkippableHelpPreconditions = new List<string>();
            Provider.GetRequiredService<DeveloperSettings>().SetDeveloperSettings(settings);

            await ReplyAsync("Set.");
        }

        [Command("ViewHelpPreconditionSkips")]
        public async Task ViewHelpPreconditionSkipsAsync()
        {
            var settings = Provider.GetRequiredService<DeveloperSettings>().GetDeveloperSettings();
            await ReplyAsync("Settings:\n" +
                $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("SetPlaying")]
        public async Task SetGame([Remainder] string game)
        {
            await Context.Client.SetActivityAsync(new Game(game));
        }

        [Command("EmulateUser")]
        public async Task SetGame(SocketGuildUser user, [Remainder]string message)
        {
            await Context.Message.DeleteAsync();
            var wh = await (Context.Channel as SocketTextChannel).CreateWebhookAsync("Hook");
            var client = new DiscordWebhookClient(wh);
            await client.SendMessageAsync(message, false, null, user.Nickname ?? user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
        }

        [Command("GetInvite")]
        public async Task GrabInviteAsync(ulong guildId)
        {
            var guild = Context.Client.GetGuild(guildId);
            if (guild == null)
            {
                await ReplyAsync("Cannot get guild.");
                return;
            }

            try
            {
                var invites = await guild.GetInvitesAsync();
                var filtered = invites.Where(x => x.IsRevoked == false);
                if (filtered.Any())
                {
                    await ReplyAsync(string.Join("\n", filtered.Select(x => x.Url)).FixLength(256));
                    return;
                }
                else
                {
                    await TryGenerateInvite(guild);
                }
            }
            catch (Exception e)
            {
                await TryGenerateInvite(guild);
            }
        }

        public async Task TryGenerateInvite(SocketGuild guild)
        {
            if (guild.CurrentUser.GuildPermissions.CreateInstantInvite)
            {
                IInviteMetadata invite = null;
                foreach (var channel in guild.TextChannels)
                {
                    try
                    {
                        invite = await channel.CreateInviteAsync();
                        break;
                    }
                    catch
                    {

                    }
                }

                if (invite == null)
                {
                    await ReplyAsync($"Unable to retreive invite for {guild.Name}");
                    return;
                }

                await ReplyAsync($"Invite Created: {invite.Url}");
            }
            else
            {
                await ReplyAsync($"Cannot generate invites in: {guild.Name}");
            }
        }

        [Command("SetLoggerChannel")]
        [RavenRequireContext(ContextType.Guild)]
        [RavenRequireBotPermission(ChannelPermission.SendMessages)]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task SetLoggerChannelAsync(SocketTextChannel channel = null)
        {
            var originalConfig = Provider.GetRequiredService<LogHandler>().GetLoggerConfig();
            if (channel == null)
            {
                await ReplyAsync("Removed logger channel");
                originalConfig.ChannelId = 0;
                originalConfig.GuildId = 0;
                originalConfig.LogToChannel = false;
                Provider.GetRequiredService<LogHandler>().SetLoggerConfig(originalConfig);
                return;
            }

            originalConfig.GuildId = channel.Guild.Id;
            originalConfig.ChannelId = channel.Id;
            originalConfig.LogToChannel = true;
            Provider.GetRequiredService<LogHandler>().SetLoggerConfig(originalConfig);
            await ReplyAsync($"Set Logger channel to {channel.Mention}");
        }
    }
}