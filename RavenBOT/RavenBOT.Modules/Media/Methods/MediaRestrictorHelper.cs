using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Common.Services;

namespace RavenBOT.Modules.Media.Methods
{
    public class MediaRestrictorHelper : IServiceable
    {
        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public LocalManagementService Local { get; }

        public MediaRestrictorHelper(IDatabase database, DiscordShardedClient client, LocalManagementService local)
        {
            Database = database;
            Client = client;
            Local = local;
            Client.MessageReceived += MessageReceivedAsync;
        }

        private async Task MessageReceivedAsync(SocketMessage discordMessage)
        {
            if (!(discordMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }

            if (!(message.Channel is SocketTextChannel channel) || !(message.Author is SocketGuildUser user))
            {
                return;
            }

            //Ensure the bot actually has permissions to delete messages
            if (channel.Guild == null || channel.Guild.CurrentUser == null || channel.Guild.CurrentUser.GuildPermissions.ManageMessages != true)
            {
                return;
            }

            if (!Local.LastConfig.IsAcceptable(channel.Guild.Id))
            {
                return;
            }

            var config = GetConfig(channel.Id);
            if (config == null)
            {
                return;
            }

            if (config.WhitelistedRoleIds.Any())
            {
                //ensure the user isn't whitelisted before deleting messages
                if (user.Roles.Any(x => config.WhitelistedRoleIds.Contains(x.Id)))
                {
                    return;
                }
            }

            var urlRegex = new Regex(@"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)");
            if (!message.Attachments.Any() && !urlRegex.Match(message.Content).Success)
            {
                await message.DeleteAsync();
            }
        }

        public class MediaRestrictedChannel
        {
            public static string DocumentName(ulong channelId) => $"MediaRestricted-{channelId}";
            public ulong ChannelId { get; set; }
            public List<ulong> WhitelistedRoleIds { get; set; } = new List<ulong>();
        }

        public MediaRestrictedChannel GetOrCreateConfig(ulong channelId)
        {
            var config = GetConfig(channelId);
            if (config == null)
            {
                config = new MediaRestrictedChannel();
                config.ChannelId = channelId;
                SaveConfig(config);
            }

            return config;
        }

        public void SaveConfig(MediaRestrictedChannel config)
        {
            Database.Store(config, MediaRestrictedChannel.DocumentName(config.ChannelId));
        }

        public MediaRestrictedChannel GetConfig(ulong channelId)
        {
            return Database.Load<MediaRestrictedChannel>(MediaRestrictedChannel.DocumentName(channelId));
        }
    }
}