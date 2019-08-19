using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RavenBOT.Common;
using RavenBOT.Modules.RoleManagement.Models;

namespace RavenBOT.Modules.RoleManagement.Methods
{
    public class RoleManager : IServiceable
    {
        public RoleManager(IDatabase database, HttpClient httpClient, DiscordShardedClient client, LocalManagementService local)
        {
            Database = database;
            HttpClient = httpClient;
            Client = client;
            Local = local;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        public IDatabase Database { get; }
        public HttpClient HttpClient { get; }
        public DiscordShardedClient Client { get; }
        public LocalManagementService Local { get; }

        public int IsUnicodeNumberEmote(string name)
        {
            if (name == null) return -1;

            for (int i = 1; i < 9; i++)
            {
                if (name.Equals($"{i}\U000020e3"))
                {
                    return i;
                }
            }

            return -1;
        }

        public enum SubscriptionStatus
        {
            Error,
            NotSubscribed,
            Subscribed,
            Unknown
        }

        public async Task<SubscriptionStatus> IsSubscribedTo(string user, string subbedTo)
        {
            var config = Database.Load<YoutubeConfig>(YoutubeConfig.DocumentName());
            if (config == null)
            {
                return SubscriptionStatus.Unknown;
            }

            var parameters = $"?part=snippet%2CcontentDetails&forChannelId={subbedTo}&channelId={user}&key={config.ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.googleapis.com/youtube/v3/subscriptions{parameters}");

            var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return SubscriptionStatus.Error;
            }

            var content = await response.Content.ReadAsStringAsync();
            var token = JToken.Parse(content);
            var firstMatch = token.Value<JToken>("items").FirstOrDefault();
            if (firstMatch == null)
            {
                return SubscriptionStatus.NotSubscribed;
            }

            if (firstMatch.Value<JToken>("snippet").Value<JToken>("resourceId").Value<JToken>("channelId").ToString().Equals(subbedTo))
            {
                return SubscriptionStatus.Subscribed;
            }

            return SubscriptionStatus.Unknown;
        }

        private async Task RunReaction(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction, bool added)
        {

            var unicodeNumberResult = IsUnicodeNumberEmote(reaction.Emote.Name);
            if (unicodeNumberResult == -1)
            {
                return;
            }

            if (!(channel is ITextChannel tChannel))
            {
                return;
            }

            if (!Local.LastConfig.IsAcceptable(tChannel.Guild?.Id ?? 0))
            {
                return;
            }

            if (!reaction.User.IsSpecified)
            {
                return;
            }

            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook)
            {
                return;
            }

            var message = await TryGetMessage(cache, tChannel, reaction);
            if (message == null)
            {
                return;
            }

            if (!message.Author.IsBot)
            {
                return;
            }

            var config = GetConfig(tChannel.GuildId);
            if (config == null)
            {
                return;
            }

            var match = config.RoleMessages.FirstOrDefault(x => x.MessageId == cache.Id);
            if (match == null)
            {
                return;
            }

            if (unicodeNumberResult > match.Roles.Count)
            {
                return;
            }

            var roleMatch = match.Roles[unicodeNumberResult - 1];
            var role = tChannel.Guild.GetRole(roleMatch);

            if (role == null)
            {
                return;
            }

            var bot = await tChannel.Guild.GetCurrentUserAsync();
            if (!(bot is SocketGuildUser gBot) || !gBot.GuildPermissions.ManageRoles || role.Position >= gBot.Hierarchy || !(reaction.User.Value is IGuildUser gUser))
            {
                return;
            }

            if (added)
            {
                await gUser.AddRoleAsync(role);
            }
            else
            {
                await gUser.RemoveRoleAsync(role);
            }
        }

        private async Task ReactionRemoved(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await RunReaction(cache, channel, reaction, false);
        }

        private async Task ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await RunReaction(cache, channel, reaction, true);
        }

        public async Task<IUserMessage> TryGetMessage(Cacheable<IUserMessage, ulong> messageCacheable, ITextChannel channel, SocketReaction reaction)
        {
            IUserMessage message;
            if (messageCacheable.HasValue)
            {
                message = messageCacheable.Value;
            }
            else
            {
                var iMessage = await channel.GetMessageAsync(messageCacheable.Id);
                if (iMessage is IUserMessage uMessage)
                {
                    message = uMessage;
                }
                else
                {
                    return null;
                }
            }

            return message;
        }

        public YoutubeRoleConfig GetOrCreateYTConfig(ulong guildId)
        {
            var config = GetYTConfig(guildId);
            if (config == null)
            {
                config = new YoutubeRoleConfig();
                config.GuildId = guildId;
                SaveYTConfig(config);
            }

            return config;
        }

        public void SaveYTConfig(YoutubeRoleConfig config)
        {
            Database.Store(config, YoutubeRoleConfig.DocumentName(config.GuildId));
        }

        public YoutubeRoleConfig GetYTConfig(ulong guildId)
        {
            return Database.Load<YoutubeRoleConfig>(YoutubeRoleConfig.DocumentName(guildId));
        }

        public RoleConfig GetOrCreateConfig(ulong guildId)
        {
            var config = GetConfig(guildId);
            if (config == null)
            {
                config = new RoleConfig();
                config.GuildId = guildId;
                SaveConfig(config);
            }

            return config;
        }

        public void SaveConfig(RoleConfig config)
        {
            Database.Store(config, RoleConfig.DocumentName(config.GuildId));
        }

        public RoleConfig GetConfig(ulong guildId)
        {
            return Database.Load<RoleConfig>(RoleConfig.DocumentName(guildId));
        }
    }
}