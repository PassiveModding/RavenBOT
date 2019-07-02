using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Modules.Moderator.Models;

namespace RavenBOT.Modules.Moderator.Methods
{
    public class ModerationHandler : IServiceable
    {
        public IDatabase Database { get; }

        public TimeTracker TimedActions { get; set; }

        public DiscordShardedClient Client { get; }

        public Timer Timer { get; set; }
        public ModerationHandler(IDatabase database, ShardChecker checker, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            TimedActions = Database.Load<TimeTracker>(TimeTracker.DocumentName);

            if (TimedActions == null)
            {
                TimedActions = new TimeTracker();
                Database.Store(TimedActions, TimeTracker.DocumentName);
            }

            checker.AllShardsReady += () =>
            {
                Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Tries to get the mute role, if unsuccessful creats a new one.
        /// Will automatically apply relevant permissions for all channels in the guild
        /// </summary>
        /// <param name="config"></param>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<IRole> GetOrCreateMuteRole(ActionConfig config, SocketGuild guild)
        {
            IRole role;
            if (config.MuteRole == 0)
            {
                //If the role isn't set just create it
                role = await guild.CreateRoleAsync("Muted");
                config.MuteRole = role.Id;
                Save(config, ActionConfig.DocumentName(guild.Id));
            }
            else
            {
                role = guild.GetRole(config.MuteRole);
                if (role == null)
                {
                    //In the case that the role was removed or is otherwise unavailable generate a new one
                    role = await guild.CreateRoleAsync("Muted");
                    config.MuteRole = role.Id;
                    Save(config, ActionConfig.DocumentName(guild.Id));
                }

                //Set the default role permissions to deny the only ways the user can communicate in the server
                if (role.Permissions.SendMessages || role.Permissions.AddReactions || role.Permissions.Connect || role.Permissions.Speak)
                {
                    await role.ModifyAsync(x =>
                    {
                        x.Permissions = new GuildPermissions(sendMessages: false, addReactions: false, connect: false, speak: false);
                    });
                }
            }

            foreach (var channel in guild.Channels)
            {
                //Update channel permission overwrites to stop the user from being able to communicate
                if (channel.PermissionOverwrites.All(x => x.TargetId != role.Id))
                {
                    var _ = Task.Run(async() => await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny, connect: PermValue.Deny, speak: PermValue.Deny)));
                }
            }

            return role;
        }

        public void TimerEvent(object _)
        {
            var run = Task.Run(async() =>
            {
                var now = DateTime.UtcNow;
                var modified = false;
                foreach (var user in TimedActions.Users.ToList())
                {
                    if (user.TimeStamp + user.Length < now)
                    {
                        var discordUser = Client.GetGuild(user.GuildId)?.GetUser(user.UserId);
                        if (discordUser == null)
                        {
                            continue;
                        }

                        //TODO: Use context free log to track unmutes and softban removals
                        if (user.Action == TimeTracker.User.TimedAction.Mute)
                        {
                            //Remove mute role from user.
                            var config = GetActionConfig(user.GuildId);
                            var role = discordUser.Guild.GetRole(config.MuteRole);
                            if (role == null)
                            {
                                continue;
                            }

                            await discordUser.RemoveRoleAsync(role);
                        }
                        else if (user.Action == TimeTracker.User.TimedAction.SoftBan)
                        {
                            //Get server, unban user.
                            await discordUser.Guild.RemoveBanAsync(user.UserId);
                        }

                        //Test if removing works
                        TimedActions.Users.Remove(user);
                        modified = true;
                    }
                }

                if (modified)
                {
                    Save(TimedActions, TimeTracker.DocumentName);
                }
            });
        }

        public void SaveModeratorConfig(ModeratorConfig config)
        {
            Database.Store(config, ModeratorConfig.DocumentName(config.GuildId));
        }

        public ModeratorConfig GetOrCreateModeratorConfig(ulong guildId)
        {
            var config = GetModeratorConfig(guildId);
            if (config == null)
            {
                config = new ModeratorConfig
                {
                GuildId = guildId
                };
                SaveModeratorConfig(config);
            }

            return config;
        }

        public ModeratorConfig GetModeratorConfig(ulong guildId)
        {
            return Database.Load<ModeratorConfig>(ModeratorConfig.DocumentName(guildId));
        }

        public ActionConfig GetActionConfig(ulong guildId)
        {
            var config = Database.Load<ActionConfig>(ActionConfig.DocumentName(guildId));
            if (config == null)
            {
                config = new ActionConfig(guildId);
                Database.Store(config, ActionConfig.DocumentName(guildId));
            }

            return config;
        }

        public ActionConfig.ActionUser GetActionUser(ulong guildId, ulong userId)
        {
            var config = Database.Load<ActionConfig.ActionUser>(ActionConfig.ActionUser.DocumentName(userId, guildId));
            if (config == null)
            {
                config = new ActionConfig.ActionUser(userId, guildId);
                Database.Store(config, ActionConfig.ActionUser.DocumentName(userId, guildId));
            }

            return config;
        }

        public async Task LogMessageAsync(ShardedCommandContext context, string message, ulong actionedUserId, string reason = null, bool logRecentMessages = true)
        {
            var embed = new EmbedBuilder();
            var gUser = context.Guild.GetUser(actionedUserId);
            if (gUser != null)
            {
                await LogMessageAsync(context, message, gUser, reason, logRecentMessages);
                return;
            }

            embed.Author = new EmbedAuthorBuilder
            {
                Name = $"User: [{actionedUserId}]"
            };

            await LogMessageFinalAsync(embed, context, actionedUserId, message, reason, logRecentMessages);
        }

        private async Task ContextFreeLogMessageFinalAsync(SocketGuild guild, IUser moderator, IUser target, string message, string reason = null)
        {
            var config = GetActionConfig(guild.Id);
            if (config.LogChannelId == 0)
            {
                //TODO: Optionally also send message to churrent channel
                return;
            }

            var logChannel = guild.GetTextChannel(config.LogChannelId);
            if (logChannel == null)
            {
                return;
            }

            var fields = new List<EmbedFieldBuilder>()
            {
                new EmbedFieldBuilder
                {
                Name = "Action",
                Value = message?.FixLength(1023) ?? "N/A"
                },
                new EmbedFieldBuilder
                {
                Name = "Reason",
                Value = reason?.FixLength(1023) ?? "N/A"
                },
                new EmbedFieldBuilder
                {
                Name = "Moderator",
                Value = $"{moderator.Username}#{moderator.Discriminator} ({moderator.Id}/{moderator.Mention})"
                }
            };

            var embed = new EmbedBuilder();
            embed.Fields = fields;
            embed = embed.WithCurrentTimestamp();

            await logChannel.SendMessageAsync("", false, embed.Build());
        }

        private async Task LogMessageFinalAsync(EmbedBuilder embed, ShardedCommandContext context, ulong actionedUserId, string message, string reason = null, bool logRecentMessages = false)
        {
            var config = GetActionConfig(context.Guild.Id);
            if (config.LogChannelId == 0)
            {
                //TODO: Optionally also send message to churrent channel
                return;
            }

            var logChannel = context.Guild.GetTextChannel(config.LogChannelId);
            if (logChannel == null)
            {
                return;
            }

            var fields = new List<EmbedFieldBuilder>()
            {
                new EmbedFieldBuilder
                {
                Name = "Action",
                Value = message?.FixLength(1023) ?? context.Message.Content?.FixLength(1023)
                },
                new EmbedFieldBuilder
                {
                Name = "Reason",
                Value = reason?.FixLength(1023) ?? "N/A"
                },
                new EmbedFieldBuilder
                {
                Name = "Moderator",
                Value = $"{context.User.Username}#{context.User.Discriminator} ({context.User.Id}/{context.User.Mention})"
                }
            };

            if (actionedUserId != 0 && logRecentMessages)
            {
                var messages = await context.Channel.GetFlattenedMessagesAsync(25);
                if (messages.Any(x => x.Author.Id == actionedUserId))
                {
                    var chatLog = string.Join("\n", messages.Select(x =>
                    {
                        if (x.Author.Id == actionedUserId)
                        {
                            return $"**{x.Author.Username}#{x.Author.Discriminator}**: {x.Content}";
                        }
                        else
                        {
                            return $"*{x.Author.Username}#{x.Author.Discriminator}*: {x.Content}";
                        }

                    }));
                    fields.Add(new EmbedFieldBuilder
                    {
                        Name = "Chat Log",
                            Value = chatLog.FixLength(1023)
                    });
                }
            }

            embed.Fields = fields;
            embed = embed.WithCurrentTimestamp();

            await logChannel.SendMessageAsync("", false, embed.Build());
        }

        public async Task LogMessageAsync(ShardedCommandContext context, string message, SocketGuildUser actionedUser, string reason = null, bool logRecentMessages = true)
        {
            var embed = new EmbedBuilder();
            if (actionedUser != null)
            {
                embed.Author = new EmbedAuthorBuilder
                {
                IconUrl = actionedUser.GetAvatarUrl(),
                Name = actionedUser.Nickname ?? $"{actionedUser.Username}#{actionedUser.Discriminator}"
                };
            }

            await LogMessageFinalAsync(embed, context, actionedUser?.Id ?? 0, message, reason, logRecentMessages);
        }

        public void Save<T>(T document, string name = null)
        {
            Database.Store(document, name);
        }
    }
}