using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Common.Services;
using RavenBOT.Modules.StatChannels.Models;

namespace RavenBOT.Modules.StatChannels.Methods
{
    public class StatChannelService : IServiceable
    {
        public StatChannelService(IDatabase database, ShardChecker checker, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            checker.AllShardsReady += () =>
            {
                Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                return Task.CompletedTask;
            };
            Client.UserJoined += UserJoined;
            Client.UserLeft += UserLeft;
        }

        public class QueueObject
        {
            public DateTime Time { get; set; } = DateTime.UtcNow;
            public SocketGuild Guild { get; set; }
            public SocketVoiceChannel Channel { get; set; }
            public string NewName { get; set; }
        }

        public List<QueueObject> Queue { get; set; } = new List<QueueObject>();

        public void TimerEvent(object _)
        {
            Task.Run(() =>
            {
                //Clear out the list so duplicates arent used if the event runs again
                var items = Queue.ToList();
                Queue.Clear();
                foreach (var guildUpdateGroup in items.GroupBy(x => x.Guild.Id))
                {
                    //Use channel ID to separate update value types.
                    foreach (var typeGroup in guildUpdateGroup.GroupBy(x => x.Channel.Id))
                    {
                        //Find only the most recent event to use so that channels aren't being updated as frequently
                        var maxTime = typeGroup.Max(x => x.Time);
                        var maxEvent = typeGroup.First(x => x.Time == maxTime);
                        maxEvent.Channel.ModifyAsync(x => x.Name = maxEvent.NewName);
                    }
                }
            }).ConfigureAwait(false);
        }

        public Task UserLeft(SocketGuildUser user)
        {
            var config = GetConfig(user.Guild.Id);
            if (config == null)
            {
                return Task.CompletedTask;
            }

            if (config.UserCountChannelId != 0)
            {
                var channel = user.Guild.GetVoiceChannel(config.UserCountChannelId);
                if (channel == null)
                {
                    config.UserCountChannelId = 0;
                    SaveConfig(config);
                    return Task.CompletedTask;
                }

                Queue.Add(new QueueObject()
                {
                    Guild = user.Guild,
                        Channel = channel,
                        NewName = $"👥 Members: {user.Guild.MemberCount}"
                });
            }

            return Task.CompletedTask;
        }

        public Task UserJoined(SocketGuildUser user)
        {
            var config = GetConfig(user.Guild.Id);
            if (config == null)
            {
                return Task.CompletedTask;
            }

            if (config.UserCountChannelId != 0)
            {
                var channel = user.Guild.GetVoiceChannel(config.UserCountChannelId);
                if (channel == null)
                {
                    config.UserCountChannelId = 0;
                    SaveConfig(config);
                    return Task.CompletedTask;
                }

                Queue.Add(new QueueObject()
                {
                    Guild = user.Guild,
                        Channel = channel,
                        NewName = $"👥 Members: {user.Guild.MemberCount}"
                });
            }

            return Task.CompletedTask;
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public Timer Timer { get; set; }

        public StatConfig GetConfig(ulong guildId)
        {
            return Database.Load<StatConfig>(StatConfig.DocumentName(guildId));
        }

        public StatConfig GetOrCreateConfig(ulong guildId)
        {
            var config = GetConfig(guildId);
            if (config == null)
            {
                config = new StatConfig(guildId);
                SaveConfig(config);
            }

            return config;
        }

        public void SaveConfig(StatConfig config)
        {
            Database.Store(config, StatConfig.DocumentName(config.GuildId));
        }
    }
}