using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.Lithium.Models.Events;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Lithium.Methods
{
    public class EventService
    {
        public DiscordShardedClient Client { get; }
        public IDatabase Database { get; }

        public EventService(DiscordShardedClient client, IDatabase database)
        {
            Client = client;
            Database = database;
            Configs = new Dictionary<ulong, EventConfig>();
            
            Client.ChannelCreated += Client_ChannelCreated;
            Client.ChannelDestroyed += Client_ChannelDestroyed;
            Client.ChannelUpdated += Client_ChannelUpdated;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.UserJoined += Client_UserJoined;
            Client.UserLeft += Client_UserLeft;
            Client.GuildMemberUpdated += GuildMemberUpdated;
        }

        private async Task GuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            //TODO: Reduce amount of lag this produces

            var config = GetConfig(userBefore.Guild.Id);
            if (!config.Enabled || config.ChannelId == 0 || !config.UserUpdated)
            {
                return;
            }

            var builder = new StringBuilder();
            if (userBefore.Username != userAfter.Username)
            {
                builder.AppendLine($"**Username:** {userBefore.Username} => {userAfter.Username}");
            }

            if (userBefore.Discriminator != userAfter.Discriminator)
            {
                builder.AppendLine($"**Discriminator:** {userBefore.Discriminator} => {userAfter.Discriminator}");
            }

            if (userBefore.Activity?.Name != userAfter.Activity?.Name)
            {
                builder.AppendLine($"**Activity:** {userBefore.Activity?.Name ?? "N/A"} => {userAfter.Activity?.Name ?? "N/A"}");
            }

            if (userBefore.Activity?.Name != userAfter.Activity?.Name)
            {
                builder.AppendLine($"**Activity Type:** {userBefore.Activity?.Type} => {userAfter.Activity?.Type}");
            }

            if (userBefore.Status != userAfter.Status)
            {
                builder.AppendLine($"**Status:** {userBefore.Status} => {userAfter.Status}");
            }
            
            if (userBefore.Hierarchy != userAfter.Hierarchy)
            {
                builder.AppendLine($"**Hierarchy:** {userBefore.Hierarchy} => {userAfter.Hierarchy}");
            }

            if (userBefore.Nickname != userAfter.Nickname)
            {
                builder.AppendLine($"**Nickname:** {userBefore.Nickname} => {userAfter.Nickname}");
            }

            //Roles lost
            var lost = userBefore.Roles.Where(x => userAfter.Roles.All(a => a.Id != x.Id)).ToList();

            //Roles Gained
            var gained = userAfter.Roles.Where(x => userBefore.Roles.All(a => a.Id != x.Id)).ToList();

            if (lost.Any())
            {
                builder.AppendLine($"**Roles Removed:** {string.Join("\n", lost.Select(x => x.Mention))}");
            }

            if (gained.Any())
            {
                builder.AppendLine($"**Roles Added:** {string.Join("\n", gained.Select(x => x.Mention))}");
            }

            var permissionsLost = userBefore.GuildPermissions.ToList().Where(x => userAfter.GuildPermissions.ToList().All(p => x != p)).ToList();
            var permissionsGained = userAfter.GuildPermissions.ToList().Where(x => userBefore.GuildPermissions.ToList().All(p => x != p)).ToList();

            if (permissionsLost.Any())
            {
                builder.AppendLine($"**Permissions Removed:** {string.Join("\n", permissionsLost.Select(x => x.ToString()))}");
            }
            
            if (permissionsGained.Any())
            {
                builder.AppendLine($"**Permissions Gained:** {string.Join("\n", permissionsGained.Select(x => x.ToString()))}");
            }

            if (builder.Length == 0)
            {
                return;
            }

            builder.AppendLine($"**{userAfter.Nickname ?? userAfter.Username} Updated**");

            var embed = new EmbedBuilder
            {
                Title = "User Updated",
                Description = builder.ToString()
            };

            await LogEvent(config, embed);
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            var config = GetConfig(user.Guild.Id);
            if (!config.Enabled || config.ChannelId == 0 || !config.UserLeft)
            {
                return;
            }
            
            await LogEvent(config, new EmbedBuilder
            {
                Title = "User Left",
                Description = $"Name: {user.Username}#{user.Discriminator}\n" +
                              $"Nickname: {user.Nickname ?? "N/A"}\n" +
                              $"ID: {user.Id}\n" +
                              $"Mention: {user.Mention}",
                Color = Color.DarkOrange
            });
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            var config = GetConfig(user.Guild.Id);
            if (!config.Enabled || config.ChannelId == 0 || !config.UserJoined)
            {
                return;
            }

            await LogEvent(config, new EmbedBuilder
            {
                Title = "User Joined",
                Description = $"Name: {user.Username}#{user.Discriminator}\n" +
                              $"Nickname: {user.Nickname ?? "N/A"}\n" +
                              $"ID: {user.Id}\n" +
                              $"Mention: {user.Mention}",
                Color = Color.Green
            });
        }

        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> messageOldCache, SocketMessage messageNew, ISocketMessageChannel messageChannel)
        {
            if (messageChannel is SocketGuildChannel gChannel)
            {
                var config = GetConfig(gChannel.Guild.Id);
                if (!config.Enabled || config.ChannelId == 0 || !config.MessageUpdated)
                {
                    return;
                }

                if (messageNew.Author.IsBot)
                {
                    //To stop bot events from being logged as they are often updated
                    return;
                }

                //TODO: Embeds although the only instances where embeds would be updated would be from bots, which are being ignored above.
                if (messageOldCache.HasValue)
                {
                    var oldMessage = messageOldCache.Value.Content;

                    await LogEvent(config, new EmbedBuilder
                    {
                        Title = "Message Updated",
                        Description = $"**Author:** {messageNew.Author.Mention}\n" +
                                      "**Old:**\n" +
                                      $"{oldMessage}\n" +
                                      "**New:**\n" +
                                      $"{messageNew.Content}\n" +
                                      $"**Channel:** {messageChannel.Name}"
                    });
                }
            }
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel messageChannel)
        {
            if (messageChannel is SocketGuildChannel gChannel)
            {
                var config = GetConfig(gChannel.Guild.Id);
                if (!config.Enabled || config.ChannelId == 0 || !config.MessageDeleted)
                {
                    return;
                }

                if (messageCache.HasValue)
                {
                    var oldMessage =  messageCache.Value.Content;

                    await LogEvent(config, new EmbedBuilder
                    {
                        Title = "Message Deleted",
                        Description = "**Message:**\n" +
                                      $"{oldMessage}\n" +
                                      $"**Channel:** {messageChannel.Name}\n" +
                                      $"**Author:** {messageCache.Value.Author.Username}#{messageCache.Value.Author.Discriminator}",
                        Color = Color.DarkTeal
                    });
                }
                else
                {
                    await LogEvent(config, new EmbedBuilder
                    {
                        Title = "Message Deleted",
                        Description = "**Message:**\n" +
                                      $"Unable to be retrieved ({messageCache.Id})\n" +
                                      $"**Channel:** {messageChannel.Name}",
                        Color = Color.DarkTeal
                    });
                }
            }
        }

        private async Task Client_ChannelUpdated(SocketChannel channelBefore, SocketChannel channelAfter)
        {
            if (channelBefore is SocketGuildChannel gChannel)
            {
                var config = GetConfig(gChannel.Guild.Id);
                if (!config.Enabled || config.ChannelId == 0 || !config.ChannelUpdated)
                {
                    return;
                }

                if (channelBefore is SocketTextChannel textChannelBefore && channelAfter is SocketTextChannel textChannelAfter)
                {
                    var builder = new StringBuilder();

                    if (textChannelBefore.Name != textChannelAfter.Name)
                    {
                        builder.AppendLine($"**Name:** {textChannelBefore.Name} => {textChannelAfter.Name}");
                    }

                    if (textChannelBefore.Topic != textChannelAfter.Topic)
                    {
                        builder.AppendLine($"**Topic:** {textChannelBefore.Topic} => {textChannelAfter.Topic}");
                    }

                    if (textChannelBefore.Category?.Id != textChannelAfter.Category?.Id)
                    {
                        builder.AppendLine($"**Category:** {textChannelBefore.Category?.Name ?? "N/A"} => {textChannelAfter.Category?.Name ?? "N/A"}");
                    }
                    
                    if (textChannelBefore.IsNsfw != textChannelAfter.IsNsfw)
                    {
                        builder.AppendLine($"**NSFW:** {textChannelBefore.IsNsfw} => {textChannelAfter.IsNsfw}");
                    }

                    if (textChannelBefore.SlowModeInterval != textChannelAfter.SlowModeInterval)
                    {
                        builder.AppendLine($"**Slow Mode Interval:** {textChannelBefore.SlowModeInterval} => {textChannelAfter.SlowModeInterval}");
                    }


                    var permissionsAdded = textChannelAfter.PermissionOverwrites.Where(x => textChannelBefore.PermissionOverwrites.All(bef => bef.TargetId != x.TargetId)).ToList();
                    if (permissionsAdded.Any())
                    {
                        builder.AppendLine("**Permissions Added:**\n" +
                                           $"{PermissionList(textChannelAfter, permissionsAdded)}");
                    }
                    
                    var permissionsRemoved = textChannelBefore.PermissionOverwrites.Where(x => textChannelAfter.PermissionOverwrites.All(bef => bef.TargetId != x.TargetId)).ToList();
                    if (permissionsRemoved.Any())
                    {
                        builder.AppendLine("**Permissions Removed:**\n" +
                                           $"{PermissionList(textChannelAfter, permissionsRemoved)}");
                    }

                    if (builder.Length == 0)
                    {
                        return;
                    }

                    await LogEvent(config, new EmbedBuilder
                    {
                        Title = "Channel Updated",
                        Description = builder.ToString().FixLength(2047)
                    });
                }
            }
        }

        private async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            if (channel is SocketGuildChannel gChannel)
            {
                var config = GetConfig(gChannel.Guild.Id);
                if (!config.Enabled || config.ChannelId == 0 || !config.ChannelDeleted)
                {
                    return;
                }

                if (channel is SocketTextChannel tChannel)
                {
                    var embed = new EmbedBuilder
                    {
                        Title = "Channel Destroyed",
                        Color = Color.Red,
                        Description = $"Name: {tChannel.Name}\n" +
                                      $"Topic: {tChannel.Topic ?? "N/A"}\n" +
                                      $"NSFW: {tChannel.IsNsfw}\n" +
                                      $"SlowModeInterval: {tChannel.SlowModeInterval}\n" +
                                      $"Category: {tChannel.Category?.Name ?? "N/A"}\n" +
                                      $"Position: {tChannel.Position}\n" +
                                      $"Permissions:\n{PermissionList(gChannel, tChannel.PermissionOverwrites.ToList())}".FixLength(2047)
                    };

                    await LogEvent(config, embed);
                }
                else if (channel is SocketVoiceChannel vChannel)
                {
                    var embed = new EmbedBuilder
                    {
                        Title = "Channel Destroyed",
                        Color = Color.Red,
                        Description = $"Name: {vChannel.Name}\n" +
                                      $"Category: {vChannel.Category?.Name ?? "N/A"}\n" +
                                      $"User Limit: {(vChannel.UserLimit == null ? "N/A" : vChannel.UserLimit.ToString())}\n" +
                                      $"BitRate: {vChannel.Bitrate}\n" +
                                      $"Position: {vChannel.Position}\n" +
                                      $"Permissions:\n{PermissionList(gChannel, vChannel.PermissionOverwrites.ToList())}".FixLength(2047)
                    };

                    await LogEvent(config, embed);
                }
            }
        }

        private async Task Client_ChannelCreated(SocketChannel channel)
        {
            if (channel is SocketGuildChannel gChannel)
            {
                var config = GetConfig(gChannel.Guild.Id);
                if (!config.Enabled || config.ChannelId == 0 || !config.ChannelCreated)
                {
                    return;
                }

                if (channel is SocketTextChannel tChannel)
                {
                    var embed = new EmbedBuilder
                    {
                        Title = "Channel Created",
                        Color = Color.Green,
                        Description = $"Name: {tChannel.Name}\n" +
                                      $"Topic: {tChannel.Topic ?? "N/A"}\n" +
                                      $"NSFW: {tChannel.IsNsfw}\n" +
                                      $"SlowModeInterval: {tChannel.SlowModeInterval}\n" +
                                      $"Category: {tChannel.Category?.Name ?? "N/A"}\n" +
                                      $"Position: {tChannel.Position}\n" +
                                      $"Permissions: {PermissionList(gChannel, tChannel.PermissionOverwrites.ToList())}".FixLength(2047)
                    };

                    await LogEvent(config, embed);
                }
                else if (channel is SocketVoiceChannel vChannel)
                {
                    var embed = new EmbedBuilder
                    {
                        Title = "Channel Created",
                        Color = Color.Green,
                        Description = $"Name: {vChannel.Name}\n" +
                                      $"Category: {vChannel.Category?.Name ?? "N/A"}\n" +
                                      $"User Limit: {(vChannel.UserLimit == null ? "N/A" : vChannel.UserLimit.ToString())}\n" +
                                      $"BitRate: {vChannel.Bitrate}\n" +
                                      $"Position: {vChannel.Position}\n" +
                                      $"Permissions: {PermissionList(gChannel, vChannel.PermissionOverwrites.ToList())}".FixLength(2047)
                    };

                    await LogEvent(config, embed);
                }
            }
        }

        public string PermissionList(SocketGuildChannel channel, List<Overwrite> permissions)
        {
            var builder = new StringBuilder();
            foreach (var permission in permissions)
            {
                if (permission.TargetType == PermissionTarget.Role)
                {
                    var role = channel.Guild.GetRole(permission.TargetId);
                    if (role != null)
                    {
                        builder.AppendLine($"**Role:** {role.Mention}");
                        if (permission.Permissions.ToAllowList().Any())
                        {
                            builder.AppendLine("**Allowed Permissions:**\n" +
                                     $"{string.Join("\n", permission.Permissions.ToAllowList().Select(x => x.ToString()))}\n");
                        }

                        if (permission.Permissions.ToDenyList().Any())
                        {
                            builder.AppendLine("**Denied Permissions:**\n" +
                                     $"{string.Join("\n", permission.Permissions.ToDenyList().Select(x => x.ToString()))}\n");
                        }
                    }
                }
                else
                {
                    var user = channel.Guild.GetUser(permission.TargetId);
                    if (user != null)
                    {
                        builder.AppendLine($"**User:** {user.Mention}");
                        if (permission.Permissions.ToAllowList().Any())
                        {
                            builder.AppendLine("**Allowed Permissions:**\n" +
                                               $"{string.Join("\n", permission.Permissions.ToAllowList().Select(x => x.ToString()))}\n");
                        }

                        if (permission.Permissions.ToDenyList().Any())
                        {
                            builder.AppendLine("**Denied Permissions:**\n" +
                                               $"{string.Join("\n", permission.Permissions.ToDenyList().Select(x => x.ToString()))}\n");
                        }
                    }
                }
            }

            return builder.ToString();
        }

        public async Task LogEvent(EventConfig config, EmbedBuilder embed)
        {
            try
            {
                var channel = Client.GetGuild(config.GuildId)?.GetTextChannel(config.ChannelId);
                if (channel != null)
                {
                    await channel.SendMessageAsync("", false, embed.Build());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Dictionary<ulong, EventConfig> Configs { get; set; }

        public EventConfig GetConfig(ulong guildId)
        {
            if (Configs.TryGetValue(guildId, out EventConfig config))
            {
                return config;
            }

            config = Database.Load<EventConfig>(EventConfig.DocumentName(guildId));

            if (config == null)
            {
                config = new EventConfig(guildId);
                Database.Store(config, EventConfig.DocumentName(guildId));
            }

            //TODO: Check if add or tryadd
            Configs.Add(guildId, config);
            return config;
        }

        public void SaveConfig(EventConfig config)
        {
            Database.Store(config, EventConfig.DocumentName(config.GuildId));
            if (Configs.ContainsKey(config.GuildId))
            {
                Configs[config.GuildId] = config;
            }
        }
    }
}
