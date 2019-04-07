using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord.WebSocket;
using RavenBOT.Models;

namespace RavenBOT.Services
{
    public class TimerService
    {
        private DiscordShardedClient Client { get; }
        private GraphiteService Graphite { get; }
        public BotConfig Config { get; }
        private Timer Timer { get; }

        public TimerService(DiscordShardedClient client, GraphiteService graphite, BotConfig config)
        {
            Client = client;
            Graphite = graphite;
            Config = config;
            Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }

        public void Start()
        {
            Timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }

        private void TimerEvent(object _)
        {
            if (Client.Guilds.Any())
            {
                Graphite.Report($"{Config.Name}.data.guilds", Client.Guilds.Count);
                Graphite.Report($"{Config.Name}.data.users", Client.Guilds.Sum(x => x.MemberCount));
                Graphite.Report($"{Config.Name}.data.mostusers", Client.Guilds.Max(x => x.MemberCount));
            }
            
            Graphite.Report($"{Config.Name}.data.heap", Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2));
            Graphite.Report($"{Config.Name}.data.uptime", (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds);
        }
    }
}
