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
    public class ModerationService
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

        public bool CheckToxicityAsync(string message, int max)
        {
            if (Perspective == null)
            {
                return false;
            }

            try
            {
                var res = Perspective.QueryToxicity(message);
                if (res.attributeScores.TOXICITY.summaryScore.value * 100 > max)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        public async Task RunChecks(SocketUserMessage message, SocketTextChannel channel)
        {
            var guildSetup = GetModerationConfig(channel.Guild.Id);

            //TODO: Spam check

            if (guildSetup.BlockInvites)
            {
                if (Regex.IsMatch(message.Content, @"discord(?:\.gg|\.me|app\.com\/invite)\/([\w\-]+)", RegexOptions.IgnoreCase))
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.BlockIps)
            {
                if (Regex.IsMatch(message.Content, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.BlockMassMentions)
            {
                int count = 0;
                if (guildSetup.MassMentionsIncludeChannels)
                {
                    count += message.MentionedChannels.Count;
                }

                if (guildSetup.MassMentionsIncludeRoles)
                {
                    count += message.MentionedRoles.Count;
                }

                if (guildSetup.MassMentionsIncludeUsers)
                {
                    count += message.MentionedUsers.Count;
                }

                if (count > guildSetup.MaxMentions)
                {
                    await message.DeleteAsync();
                    return;
                }
            }

            if (guildSetup.UseBlacklist)
            {
                var match = guildSetup.BlacklistCheck(message.Content);
                if (match.Item1)
                {
                    await message.DeleteAsync();
                    return;
                    //TODO: Log message, matched value, regex.
                }
            }

            if (guildSetup.UsePerspective)
            {
                var res = CheckToxicityAsync(message.Content, guildSetup.PerspectiveMax);
                if (res)
                {
                    await message.DeleteAsync();
                    //TODO: Log this action and maybe reply.
                }
            }
        }
    }
}
