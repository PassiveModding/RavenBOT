using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Modules.Events.Models.Events;

namespace RavenBOT.Modules.Events.Methods
{
    public class EventService : IServiceable
    {
        public DiscordShardedClient Client { get; }
        public IDatabase Database { get; }
        public LocalManagementService LocalManagementService { get; }

        private int ChannelCreated = 0;
        private int ChannelDestroyed = 0;
        private int ChannelUpdated = 0;
        private int MessageDeleted = 0;
        private int MessageUpdated = 0;
        private int UserJoined = 0;
        private int UserLeft = 0;
        private int UserUpdated = 0;

        private Timer Timer { get; }

        public EventService(DiscordShardedClient client, IDatabase database, LocalManagementService localManagementService)
        {
            Client = client;
            Database = database;
            LocalManagementService = localManagementService;

            Client.ChannelCreated += Client_ChannelCreated;
            Client.ChannelDestroyed += Client_ChannelDestroyed;
            Client.ChannelUpdated += Client_ChannelUpdated;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.UserJoined += Client_UserJoined;
            Client.UserLeft += Client_UserLeft;
            Client.GuildMemberUpdated += GuildMemberUpdated;
            Client.MessagesBulkDeleted += MessagesBulkDeleted;

            Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        public class EventClass
        {
            public enum EventType
            {
                ChannelCreated,
                ChannelDestroyed,
                ChannelUpdated,
                MessageDeleted,
                BulkDelete,
                MessageUpdated,
                UserJoined,
                UserLeft,
                GuildMemberUpdated
            }

            public EventClass(ulong guildId, EventConfig config, string title, string content, EventType type, Color color)
            {
                GuildId = guildId;
                Config = config;
                Type = type;
                SetTitle(title);
                SetContent(content);

                Color = color;
            }

            public EventType Type { get; set; }

            public Color Color { get; set; }

            public ulong GuildId { get; set; }
            public EventConfig Config { get; set; }

            public string Title { get; private set; }

            public void SetTitle(string title)
            {
                if (String.IsNullOrWhiteSpace(title))
                {
                    title = "N/A";
                }

                title = title.FixLength(100);
                Title = title;
            }

            public string Content { get; private set; }
            public void SetContent(string content)
            {
                if (String.IsNullOrWhiteSpace(content))
                {
                    content = "N/A";
                }

                content = content.FixLength(1023);
                Content = content;
            }
        }

        public List<EventClass> EventQueue { get; set; } = new List<EventClass>();

        public class EventClassDuplicate
        {
            public int Count { get; set; }
            public EventClass Class { get; set; }
        }

        private void TimerEvent(object _)
        {
            var task = Task.Run(async() =>
            {
                try
                {
                    foreach (var eventGroup in EventQueue.GroupBy(x => x.GuildId).ToList())
                    {
                        if (!LocalManagementService.LastConfig.IsAcceptable(eventGroup.Key))
                        {
                            EventQueue.RemoveAll(x => x.GuildId == eventGroup.Key);
                            continue;
                        }

                        var mainColor = eventGroup.GroupBy(x => x.Color).OrderByDescending(x => x.Count()).FirstOrDefault()?.FirstOrDefault()?.Color ?? Color.DarkerGrey;

                        var channel = Client.GetGuild(eventGroup.FirstOrDefault()?.GuildId ?? 0)?.GetTextChannel(eventGroup.FirstOrDefault()?.Config?.ChannelId ?? 0);
                        if (channel != null)
                        {
                            var bulkEmbed = new EmbedBuilder
                            {
                            Color = mainColor
                            };

                            var typeGroups = eventGroup.GroupBy(x => x.Type).OrderByDescending(x => x.Count());

                            foreach (var typeGroup in typeGroups)
                            {
                                var typeCopies = new List<EventClassDuplicate>();

                                foreach (var eClass in typeGroup.ToList())
                                {
                                    var match = typeCopies.FirstOrDefault(x => x.Class.Content.Equals(eClass.Content) && x.Class.Title.Equals(eClass.Title));
                                    if (match != null)
                                    {
                                        match.Count = match.Count + 1;
                                    }
                                    else
                                    {
                                        typeCopies.Add(new EventClassDuplicate
                                        {
                                            Count = 1,
                                            Class = eClass
                                        });
                                    }
                                }

                                foreach (var eventClass in typeCopies)
                                {
                                    var field = new EmbedFieldBuilder();
                                    if (eventClass.Count == 1)
                                    {
                                        field.Name = eventClass.Class.Title;
                                    }
                                    else
                                    {
                                        field.Name = eventClass.Class.Title + $" x{eventClass.Count}";
                                    }
                                    field.Value = eventClass.Class.Content;
                                    if (bulkEmbed.Length + eventClass.Class.Title.Length + eventClass.Class.Content.Length >= 5000 || bulkEmbed.Fields.Count > 20)
                                    {
                                        try
                                        {
                                            await channel.SendMessageAsync("", false, bulkEmbed.Build());
                                            bulkEmbed = new EmbedBuilder
                                            {
                                                Color = mainColor
                                            };
                                        }
                                        catch (System.Exception)
                                        {
                                            //Ignore
                                        }
                                    }

                                    bulkEmbed.AddField(field);
                                    EventQueue.RemoveAll(x => x.Content.Equals(eventClass.Class.Content) && x.Title.Equals(eventClass.Class.Title) && x.GuildId == eventClass.Class.GuildId);
                                }
                            }

                            if (bulkEmbed.Fields.Any())
                            {
                                try
                                {
                                    await channel.SendMessageAsync("", false, bulkEmbed.Build());
                                }
                                catch (System.Exception)
                                {
                                    //Ignore
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        private Task MessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> messageCache, ISocketMessageChannel channel)
        {
            MessageDeleted += messageCache.Count;
            if (channel is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.MessageDeleted)
                {
                    return Task.CompletedTask;
                }
                
                var msg = messageCache.Select(x => x.HasValue ? $"{x.Value.Author.Username}#{x.Value.Author.Discriminator}: {x.Value.Content}" : $"Uncached: [{x.Id}]");
                LogEvent(config, "Bulk Message Delete",string.Join("\n", msg).FixLength(1023), EventClass.EventType.BulkDelete, Color.DarkBlue);
                /*
                foreach (var message in messageCache)
                {
                    if (messageCache.HasValue)
                    {
                        var oldMessage = messageCache.Value.Content;

                        LogEvent(config, "Message Deleted", "**Message:**\n" +
                            $"{oldMessage}\n" +
                            $"**Channel:** {messageChannel.Name}\n" +
                            $"**Author:** {messageCache.Value.Author.Username}#{messageCache.Value.Author.Discriminator}", EventClass.EventType.MessageDeleted, Color.DarkBlue);
                    }
                    else
                    {
                        LogEvent(config, "Message Deleted", "**Message:**\n" +
                            $"Unable to be retrieved ({messageCache.Id})\n" +
                            $"**Channel:** {messageChannel.Name}", EventClass.EventType.MessageDeleted, Color.DarkBlue);
                    }               
                }
                */
            }
            return Task.CompletedTask;
        }

        private Task GuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            UserUpdated++;

            var builder = new StringBuilder();
            if (userBefore.Username != userAfter.Username)
            {
                builder.AppendLine($"**Username:** {userBefore.Username} => {userAfter.Username}");
            }

            if (userBefore.Discriminator != userAfter.Discriminator)
            {
                builder.AppendLine($"**Discriminator:** {userBefore.Discriminator} => {userAfter.Discriminator}");
            }

            /*
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
            */

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
                return Task.CompletedTask;
            }

            var config = TryGetConfig(userBefore.Guild.Id);
            if (config == null || !config.Enabled || config.ChannelId == 0 || !config.UserUpdated)
            {
                return Task.CompletedTask;
            }

            LogEvent(config, $"**{userAfter.Nickname ?? userAfter.Username} Updated**", builder.ToString(), EventClass.EventType.GuildMemberUpdated, Color.DarkMagenta);
            return Task.CompletedTask;
        }

        private Task Client_UserLeft(SocketGuildUser user)
        {
            UserLeft++;

            var config = TryGetConfig(user.Guild.Id);
            if (config == null || !config.Enabled || config.ChannelId == 0 || !config.UserLeft)
            {
                return Task.CompletedTask;
            }

            LogEvent(config, "User Left", $"Name: {user.Username}#{user.Discriminator}\n" +
                $"Nickname: {user.Nickname ?? "N/A"}\n" +
                $"ID: {user.Id}\n" +
                $"Mention: {user.Mention}", EventClass.EventType.UserLeft, Color.DarkOrange);
            return Task.CompletedTask;
        }

        private Task Client_UserJoined(SocketGuildUser user)
        {
            UserJoined++;
            var config = TryGetConfig(user.Guild.Id);
            if (config == null || !config.Enabled || config.ChannelId == 0 || !config.UserJoined)
            {
                return Task.CompletedTask;
            }

            LogEvent(config, "User Joined", $"Name: {user.Username}#{user.Discriminator}\n" +
                $"Nickname: {user.Nickname ?? "N/A"}\n" +
                $"ID: {user.Id}\n" +
                $"Mention: {user.Mention}", EventClass.EventType.UserJoined, Color.Green);

            return Task.CompletedTask;
        }

        private Task Client_MessageUpdated(Cacheable<IMessage, ulong> messageOldCache, SocketMessage messageNew, ISocketMessageChannel messageChannel)
        {
            MessageUpdated++;
            if (messageChannel is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.MessageUpdated)
                {
                    return Task.CompletedTask;
                }

                if (messageNew.Author.IsBot)
                {
                    //To stop bot events from being logged as they are often updated
                    return Task.CompletedTask;
                }

                if (messageOldCache.HasValue)
                {
                    var oldMessage = messageOldCache.Value.Content;

                    if (oldMessage.Equals(messageNew.Content))
                    {
                        return Task.CompletedTask;
                    }

                    LogEvent(config, "Message Updated", $"**Author:** {messageNew.Author.Mention}\n" +
                        "**Old:**\n" +
                        $"{oldMessage}\n" +
                        "**New:**\n" +
                        $"{messageNew.Content}\n" +
                        $"**Channel:** {messageChannel.Name}", EventClass.EventType.MessageUpdated, Color.DarkPurple);
                }
            }

            return Task.CompletedTask;
        }

        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> messageCache, ISocketMessageChannel messageChannel)
        {
            MessageDeleted++;
            if (messageChannel is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.MessageDeleted)
                {
                    return Task.CompletedTask;
                }

                if (messageCache.HasValue)
                {
                    var oldMessage = messageCache.Value.Content;

                    LogEvent(config, "Message Deleted", "**Message:**\n" +
                        $"{oldMessage}\n" +
                        $"**Channel:** {messageChannel.Name}\n" +
                        $"**Author:** {messageCache.Value.Author.Username}#{messageCache.Value.Author.Discriminator}", EventClass.EventType.MessageDeleted, Color.DarkBlue);
                }
                else
                {
                    LogEvent(config, "Message Deleted", "**Message:**\n" +
                        $"Unable to be retrieved ({messageCache.Id})\n" +
                        $"**Channel:** {messageChannel.Name}", EventClass.EventType.MessageDeleted, Color.DarkBlue);
                }
            }
            return Task.CompletedTask;
        }

        private Task Client_ChannelUpdated(SocketChannel channelBefore, SocketChannel channelAfter)
        {
            ChannelUpdated++;
            if (channelBefore is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.ChannelUpdated)
                {
                    return Task.CompletedTask;
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
                        return Task.CompletedTask;
                    }

                    LogEvent(config, "Channel Updated", builder.ToString(), EventClass.EventType.ChannelUpdated, Color.DarkTeal);
                }
            }
            return Task.CompletedTask;
        }

        private Task Client_ChannelDestroyed(SocketChannel channel)
        {
            ChannelDestroyed++;
            if (channel is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.ChannelDeleted)
                {
                    return Task.CompletedTask;
                }

                if (channel is SocketTextChannel tChannel)
                {
                    LogEvent(config, "Text Channel Destroyed", $"Name: {tChannel.Name}\n" +
                        $"Topic: {tChannel.Topic ?? "N/A"}\n" +
                        $"NSFW: {tChannel.IsNsfw}\n" +
                        $"SlowModeInterval: {tChannel.SlowModeInterval}\n" +
                        $"Category: {tChannel.Category?.Name ?? "N/A"}\n" +
                        $"Position: {tChannel.Position}\n" +
                        $"Permissions:\n{PermissionList(gChannel, tChannel.PermissionOverwrites.ToList())}", EventClass.EventType.ChannelDestroyed, Color.DarkRed);
                }
                else if (channel is SocketVoiceChannel vChannel)
                {
                    LogEvent(config, "Voice Channel Destroyed", $"Name: {vChannel.Name}\n" +
                        $"Category: {vChannel.Category?.Name ?? "N/A"}\n" +
                        $"User Limit: {(vChannel.UserLimit == null ? "N/A" : vChannel.UserLimit.ToString())}\n" +
                        $"BitRate: {vChannel.Bitrate}\n" +
                        $"Position: {vChannel.Position}\n" +
                        $"Permissions:\n{PermissionList(gChannel, vChannel.PermissionOverwrites.ToList())}", EventClass.EventType.ChannelDestroyed, Color.DarkRed);
                }
            }
            return Task.CompletedTask;
        }

        private Task Client_ChannelCreated(SocketChannel channel)
        {
            ChannelCreated++;

            if (channel is SocketGuildChannel gChannel)
            {
                var config = TryGetConfig(gChannel.Guild.Id);
                if (config == null || !config.Enabled || config.ChannelId == 0 || !config.ChannelCreated)
                {
                    return Task.CompletedTask;
                }

                if (channel is SocketTextChannel tChannel)
                {
                    LogEvent(config, "Text Channel Created", $"Name: {tChannel.Name}\n" +
                        $"Topic: {tChannel.Topic ?? "N/A"}\n" +
                        $"NSFW: {tChannel.IsNsfw}\n" +
                        $"SlowModeInterval: {tChannel.SlowModeInterval}\n" +
                        $"Category: {tChannel.Category?.Name ?? "N/A"}\n" +
                        $"Position: {tChannel.Position}\n" +
                        $"Permissions: {PermissionList(gChannel, tChannel.PermissionOverwrites.ToList())}", EventClass.EventType.ChannelCreated, Color.Green);
                }
                else if (channel is SocketVoiceChannel vChannel)
                {
                    LogEvent(config, "Voice Channel Created", $"Name: {vChannel.Name}\n" +
                        $"Category: {vChannel.Category?.Name ?? "N/A"}\n" +
                        $"User Limit: {(vChannel.UserLimit == null ? "N/A" : vChannel.UserLimit.ToString())}\n" +
                        $"BitRate: {vChannel.Bitrate}\n" +
                        $"Position: {vChannel.Position}\n" +
                        $"Permissions: {PermissionList(gChannel, vChannel.PermissionOverwrites.ToList())}", EventClass.EventType.ChannelCreated, Color.Green);
                }
            }
            return Task.CompletedTask;
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

        public void LogEvent(EventConfig config, string title, string content, EventClass.EventType type, Color color)
        {
            EventQueue.Add(new EventClass(config.GuildId, config, title, content, type, color));
        }

        public EventConfig GetOrCreateConfig(ulong guildId)
        {
            var config = TryGetConfig(guildId);
            if (config == null)
            {
                config = new EventConfig(guildId);
                SaveConfig(config);
            }

            return config;
        }
        public EventConfig TryGetConfig(ulong guildId)
        {
            return Database.Load<EventConfig>(EventConfig.DocumentName(guildId));
        }

        public void SaveConfig(EventConfig config)
        {
            Database.Store(config, EventConfig.DocumentName(config.GuildId));
        }
    }
}