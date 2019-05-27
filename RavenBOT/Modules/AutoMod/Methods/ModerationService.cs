using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Modules.AutoMod.Models;
using RavenBOT.Modules.AutoMod.Models.Moderation;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.AutoMod.Methods
{
    public partial class ModerationService
    {
        private IDatabase Database { get; }

        private DiscordShardedClient Client {get;}
        public Perspective.Api Perspective { get; set; }
        //private Dictionary<ulong, ModerationConfig> ModerationConfigs { get; }

        public ModerationService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            //ModerationConfigs = new Dictionary<ulong, ModerationConfig>();
            var setupDoc = database.Load<PerspectiveSetup>(PerspectiveSetup.DocumentName());
            if (setupDoc == null)
            {
                setupDoc = new PerspectiveSetup();
                database.Store(setupDoc, PerspectiveSetup.DocumentName());
            }

            Perspective = setupDoc.PerspectiveToken != null ? new Perspective.Api(setupDoc.PerspectiveToken) : null;
            Client = client;
            Client.MessageReceived += MessageReceived;
            Client.UserJoined += UserJoined;
            //Client.GuildMemberUpdated += MemberUpdated;
        }

        public async Task MemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            //TODO: Fix null ref spam

            //Only run if username/nickname change is detected.
            if (before.Username.Equals(after.Username) && before.Nickname?.Equals(after.Nickname) == true)
            {
                return;
            }

            if (before.Nickname != null && after.Nickname == null)
            {
                //Check username.
                await CheckDisplayName(after.Username, after);
                return;
            }

            if (!before.Username.Equals(after.Username))
            {
                //check usernames
                await CheckDisplayName(after.Username, after);
                return;
            }

            if (!before.Nickname.Equals(after.Nickname))
            {
                //check nicknames
                await CheckDisplayName(after.Nickname, after);
                return;
            }
        }

        public async Task CheckDisplayName(string content, SocketGuildUser user)
        {
            var config = GetModerationConfig(user.Guild.Id);
            if (config.BlacklistUsernames)
            {
                if (!config.BlacklistedUsernames.Any())
                {
                    return;
                }

                var match = config.BlacklistedUsernames.FirstOrDefault(x =>
                {
                    if (x.Regex)
                    {
                        return Regex.IsMatch(content, x.Content, RegexOptions.IgnoreCase);
                    }

                    return content.Contains(x.Content, StringComparison.InvariantCultureIgnoreCase);
                });


                if (match != null && match.Content != null)
                {
                    if (match.Content.Equals("💩"))
                    {
                        //Note that we cannot re-modify if the name is what we're setting it to.
                        //Something something recursion.
                        return;
                    }

                    await user.ModifyAsync(x => x.Nickname = "💩");
                }
            }
        }

        public async Task UserJoined(SocketGuildUser user)
        {
            await CheckDisplayName(user.Username, user);
        }

        public async Task MessageReceived(SocketMessage socketMessage)
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
                    await RunChecks(message, channel).ConfigureAwait(false);
                }
            }
        }

        public PerspectiveSetup GetSetup()
        {
            var setupDoc = Database.Load<PerspectiveSetup>(PerspectiveSetup.DocumentName());
            if (setupDoc == null)
            {
                setupDoc = new PerspectiveSetup();
                Database.Store(setupDoc, PerspectiveSetup.DocumentName());
            }

            return setupDoc;
        }

        public void SetPerspectiveSetup(PerspectiveSetup doc)
        {
            Database.Store(doc, PerspectiveSetup.DocumentName());
        }

        public void SaveModerationConfig(ModerationConfig config)
        {
            //ModerationConfigs[config.GuildId] = config;

            Database.Store(config, ModerationConfig.DocumentName(config.GuildId));
        }

        public ModerationConfig GetModerationConfig(ulong guildId)
        {
            //if there is a cached version, return that.
            //if (ModerationConfigs.ContainsKey(guildId))
            //{
           //     return ModerationConfigs[guildId];
            //}

            //Try to load it from database, otherwise create a new one and store it.
            var document = Database.Load<ModerationConfig>(ModerationConfig.DocumentName(guildId));
            if (document == null)
            {
                document = new ModerationConfig(guildId);
                Database.Store(document, ModerationConfig.DocumentName(guildId));
            }

            //Cache the document
            //ModerationConfigs.TryAdd(document.GuildId, document);
            return document;
        }
    }
}
