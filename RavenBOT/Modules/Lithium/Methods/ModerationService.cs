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
        public Perspective.Api Perspective { get; set; }
        private Dictionary<ulong, ModerationConfig> ModerationConfigs { get; }

        public ModerationService(IDatabase database)
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
