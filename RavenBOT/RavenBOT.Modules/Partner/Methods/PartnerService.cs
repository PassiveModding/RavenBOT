using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Modules.Partner.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Partner.Methods
{
    public class PartnerService : IServiceable
    {
        public PartnerService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;

            Timer = new Timer(TimerEvent, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(60));

            Random = new Random();
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public Timer Timer { get; }
        public Random Random { get; }

        public void TimerEvent(object _)
        {
            Task.Run(() =>PartnerEvent());
        }

        public void SavePartnerConfig(PartnerConfig config)
        {
            Database.Store(config, PartnerConfig.DocumentName(config.GuildId));
        }

        public PartnerConfig GetOrCreatePartnerConfig(ulong guildId)
        {
            var config = Database.Load<PartnerConfig>(PartnerConfig.DocumentName(guildId));
            if (config == null)
            {
                config = new PartnerConfig(guildId);
                Database.Store(config, PartnerConfig.DocumentName(guildId));
            }

            return config;
        }

        public class GroupedConfig
        {
            public SocketGuild Guild { get; set; }
            public PartnerConfig Config { get; set; }
        }

        public async Task PartnerEvent()
        {
            var configs = Database.Query<PartnerConfig>();
            var sorted = configs
                .Where(x => x.Enabled)
                .Select(x => new GroupedConfig
                {
                    Guild = Client.GetGuild(x.GuildId),
                        Config = x
                })
                .Where(x => x.Guild != null)
                .ToList();
            var randomised = sorted.OrderBy(x => Random.Next()).ToList();

            foreach (var group in randomised)
            {
                var embedToSend = await group.Config.GetEmbedAsync(group.Guild);
                var receiver = sorted.FirstOrDefault(x => x.Config.GuildId != group.Config.GuildId);
                if (receiver == null)
                {
                    return;
                }
                sorted.Remove(receiver);

                var receiverChannel = receiver.Guild.GetTextChannel(receiver.Config.ReceiverChannelId);
                if (receiverChannel == null)
                {
                    continue;
                }

                //TODO: Check permissions before sending message.

                try
                {
                    await receiverChannel.SendMessageAsync("", false, embedToSend.Build());
                    group.Config.ServerCount++;
                    group.Config.UserCount += receiver.Guild.MemberCount;
                    Database.Store(group.Config, PartnerConfig.DocumentName(group.Config.GuildId));
                }
                catch
                {
                    //
                }
            }
        }
    }
}