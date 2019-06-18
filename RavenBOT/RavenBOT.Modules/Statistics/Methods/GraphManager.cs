using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Modules.Statistics.Models;

namespace RavenBOT.Modules.Statistics.Methods
{
    public class GraphManager : IServiceable
    {
        public GraphManager(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;

            var config = GetConfig();

            GraphiteService = new GraphiteService(config.GraphiteUrl);

            Client.UserJoined += UserCountChanged;
            Client.JoinedGuild += GuildCountChanged;
            Client.LeftGuild += GuildCountChanged;
            Client.UserLeft += UserCountChanged;
            Client.MessageReceived += MessageReceived;

            MessageStatistics = new MessageStats();
            Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public void TimerEvent(object _)
        {
            MessageStatistics.LoopCount++;
            Task.Run(() =>
            {
                MessageStatistics.ThisMinute = MessageStatistics.ThisMinute.Where(x => x + TimeSpan.FromMinutes(1) > DateTime.UtcNow).ToList();
                GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/Messages/Minutely", MessageStatistics.ThisMinute.Count, DateTime.UtcNow));
                GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/Messages/Session", MessageStatistics.ThisSession, DateTime.UtcNow));
                if (MessageStatistics.LoopCount % 60 == 0)
                {
                    MessageStatistics.ThisHour = MessageStatistics.ThisHour.Where(x => x + TimeSpan.FromHours(1) > DateTime.UtcNow).ToList();
                    GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/Messages/Hourly", MessageStatistics.ThisHour.Count, DateTime.UtcNow));
                }

                if (MessageStatistics.LoopCount % (60 * 24) == 0)
                {
                    MessageStatistics.ThisDay = MessageStatistics.ThisDay.Where(x => x + TimeSpan.FromDays(1) > DateTime.UtcNow).ToList();
                    GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/Messages/Daily", MessageStatistics.ThisDay.Count, DateTime.UtcNow));
                }
            });
        }

        public class MessageStats
        {
            public int LoopCount = 0;
            public List<DateTime> ThisMinute { get; set; } = new List<DateTime>();
            public List<DateTime> ThisHour { get; set; } = new List<DateTime>();
            public List<DateTime> ThisDay { get; set; } = new List<DateTime>();

            public int ThisSession { get; set; } = 0;
        }

        public async Task MessageReceived(SocketMessage message)
        {
            var now = DateTime.UtcNow;
            MessageStatistics.ThisMinute.Add(now);
            MessageStatistics.ThisHour.Add(now);
            MessageStatistics.ThisDay.Add(now);
            MessageStatistics.ThisSession++;

            //TODO: Guild/Channel Specific tracking
        }

        public async Task GuildCountChanged(SocketGuild guild)
        {
            GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/GuildCount", Client.Guilds.Count, DateTime.UtcNow));
            GraphiteService.Report(new ahd.Graphite.Datapoint($"Bot/MemberCount", Client.Guilds.Sum(x => x.MemberCount), DateTime.UtcNow));
        }

        public async Task UserCountChanged(SocketGuildUser user)
        {
            GraphiteService.Report(new ahd.Graphite.Datapoint($"Guilds/{user.Guild.Id}/UserCount", user.Guild.MemberCount, DateTime.UtcNow));
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public GraphiteService GraphiteService { get; private set; }
        public MessageStats MessageStatistics { get; private set; }
        public Timer Timer { get; private set; }

        public GraphiteConfig GetConfig()
        {
            var config = Database.Load<GraphiteConfig>(GraphiteConfig.DocumentName());
            if (config == null)
            {
                config = new GraphiteConfig();
                Database.Store(config, GraphiteConfig.DocumentName());
            }

            return config;
        }

        public void SaveConfig(GraphiteConfig config)
        {
            Database.Store(config, GraphiteConfig.DocumentName());
        }
    }
}