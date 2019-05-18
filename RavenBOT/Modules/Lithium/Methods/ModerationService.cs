using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Modules.Lithium.Models;
using RavenBOT.Modules.Lithium.Models.Moderation;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Lithium.Methods
{
    public partial class ModerationService
    {
        private IDatabase Database { get; }

        private DiscordShardedClient Client {get;}
        public Perspective.Api Perspective { get; set; }
        private Dictionary<ulong, ModerationConfig> ModerationConfigs { get; }

        private Random Random {get;}

        public ModerationService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            ModerationConfigs = new Dictionary<ulong, ModerationConfig>();
            var setupDoc = database.Load<Setup>("LithiumSetup");
            if (setupDoc == null)
            {
                setupDoc = new Setup();
                database.Store(setupDoc, "LithiumSetup");
            }

            Perspective = setupDoc.PerspectiveToken != null ? new Perspective.Api(setupDoc.PerspectiveToken) : null;
            Client = client;
            Client.MessageReceived += MessageReceived;
            Client.ChannelCreated += ChannelCreated;
            Client.UserJoined += UserJoined;
            Random = new Random();
        }

        public Task MessageReceived(SocketMessage socketMessage)
        {
            var _ = Task.Run(async () =>
            {
                if (!(socketMessage is SocketUserMessage message))
                {
                    return;
                }

                if (message.Author.IsBot || message.Author.IsWebhook)
                {
                    return;
                }

                if (message.Channel is SocketTextChannel channel)
                {
                    if (channel.Guild != null)
                    {
                        await RunChecks(message, channel);
                    }
                }
            });

            return Task.CompletedTask;
        }

        public Setup GetSetup()
        {
            var setupDoc = Database.Load<Setup>("LithiumSetup");
            if (setupDoc == null)
            {
                setupDoc = new Setup();
                Database.Store(setupDoc, "LithiumSetup");
            }

            return setupDoc;
        }

        public void SetSetup(Setup doc)
        {
            Database.Store(doc, "LithiumSetup");
        }

        public void SaveModerationConfig(ModerationConfig config)
        {
            //Update the cached version
            if (ModerationConfigs.ContainsKey(config.GuildId))
            {
                ModerationConfigs[config.GuildId] = config;
            }
            else
            {
                ModerationConfigs.TryAdd(config.GuildId, config);
            }

            Database.Store(config, ModerationConfig.DocumentName(config.GuildId));
        }

        public ModerationConfig GetModerationConfig(ulong guildId)
        {
            //if there is a cached version, return that.
            if (ModerationConfigs.ContainsKey(guildId))
            {
                if (ModerationConfigs.TryGetValue(guildId, out var cachedModeration))
                {
                    return cachedModeration;
                }
            }

            //Try to load it from database, otherwise create a new one and store it.
            var document = Database.Load<ModerationConfig>(ModerationConfig.DocumentName(guildId));
            if (document == null)
            {
                document = new ModerationConfig(guildId);
                Database.Store(document, ModerationConfig.DocumentName(guildId));
            }

            //Cache the document
            ModerationConfigs.TryAdd(document.GuildId, document);
            return document;
        }
    }
}
