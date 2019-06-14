using System.Linq;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Modules.RoleManagement.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.RoleManagement.Methods
{
    public class RoleManager : IServiceable
    {
        public RoleManager(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }

        public int IsUnicodeNumberEmote(string name)
        {
            for (int i = 1; i < 9; i++)
            {
                if (name.Equals($"{i}\U000020e3"))
                {
                    return i;
                }                
            }

            return -1;
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