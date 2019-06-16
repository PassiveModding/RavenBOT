using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RavenBOT.Common.Services
{
    public class DiscordShardedClientProxy
    {

        public DiscordShardedClientProxy(DiscordShardedClient client)
        {
            Client = client;
            Client.ChannelCreated += OnChannelCreated;
            Client.ChannelDestroyed += OnChannelDestroyed;
            Client.ChannelUpdated += OnChannelUpdated;
            Client.CurrentUserUpdated += OnCurrentUserUpdated;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildMemberUpdated += OnGuildMemberUpdated;
            Client.GuildMembersDownloaded += OnGuildMembersDownloaded;
            Client.GuildUnavailable += OnGuildUnavailable;
            Client.GuildUpdated += OnGuildUpdated;
            Client.JoinedGuild += OnJoinedGuild;
            Client.LeftGuild += OnLeftGuild;
            Client.Log += OnLog;
            Client.LoggedIn += OnLoggedIn;
            Client.LoggedOut += OnLoggedOut;
            Client.MessageDeleted += OnMessageDeleted;
            Client.MessageReceived += OnMessageReceived;
            Client.MessageUpdated += OnMessageUpdated;
            Client.ReactionAdded += OnReactionAdded;
            Client.ReactionRemoved += OnReactionRemoved;
            Client.ReactionsCleared += OnReactionsCleared;
            Client.RecipientAdded += OnRecipientAdded;
            Client.RecipientRemoved += OnRecipientRemoved;
            Client.RoleCreated += OnRoleCreated;
            Client.RoleDeleted += OnRoleDeleted;
            Client.RoleUpdated += OnRoleUpdated;
            Client.ShardConnected += OnShardConnected;
            Client.ShardDisconnected += OnShardDisconnected;
            Client.ShardLatencyUpdated += OnShardLatencyUpdated;
            Client.ShardReady += OnShardReady;
            Client.UserBanned += OnUserBanned;
            Client.UserIsTyping += OnUserIsTyping;
            Client.UserJoined += OnUserJoined;
            Client.UserLeft += OnUserLeft;
            Client.UserUnbanned += OnUserUnbanned;
            Client.UserUpdated += OnUserUpdated;
            Client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
            Client.VoiceServerUpdated += OnVoiceServerUpdated;
        }

        public List<(ClientEventType[], Func<Task<bool>>)> PreEventFunctions { get; set; } = new List<(ClientEventType[], Func<Task<bool>>)>();

        public enum ClientEventType
        {
            ChannelCreated,
            ChannelDestroyed,
            ChannelUpdated,
            CurrentUserUpdated,
            GuildAvailable,
            GuildMemberUpdated,
            GuildMembersDownloaded,
            GuildUnavailable,
            GuildUpdated,
            JoinedGuild,
            LeftGuild,
            Log,
            LoggedIn,
            LoggedOut,
            MessageDeleted,
            MessageReceived,
            MessageUpdated,
            ReactionAdded,
            ReactionRemoved,
            ReactionsCleared,
            RecipientAdded,
            RecipientRemoved,
            RoleCreated,
            RoleDeleted,
            RoleUpdated,
            ShardConnected,
            ShardDisconnected,
            ShardLatencyUpdated,
            ShardReady,
            UserBanned,
            UserIsTyping,
            UserJoined,
            UserLeft,
            UserUnbanned,
            UserUpdated,
            UserVoiceStateUpdated,
            VoiceServerUpdated,
        }

        /// <summary>
        /// Checks the result of all pore-event tasks.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>True if any event returned true</returns>
        private async Task<bool> GetPreEventTaskResult(ClientEventType type)
        {
            var result = false;
            foreach (var func in PreEventFunctions.Where(x => x.Item1.Contains(type)))
            {
                var funcResult = await func.Item2.Invoke();
                if (funcResult == true)
                {
                    result = true;
                }
            }

            return result;
        }

        private async Task OnVoiceServerUpdated(SocketVoiceServer server)
        {
            if (await GetPreEventTaskResult(ClientEventType.VoiceServerUpdated)) return;
            await VoiceServerUpdated(server);
        }

        public event Func<SocketVoiceServer, Task> VoiceServerUpdated;

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceBefore, SocketVoiceState voiceAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserVoiceStateUpdated)) return;
            await UserVoiceStateUpdated(user, voiceBefore, voiceAfter);
        }

        public event Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> UserVoiceStateUpdated;

        private async Task OnUserUpdated(SocketUser userBefore, SocketUser userAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserUpdated)) return;
            await UserUpdated(userBefore, userAfter);
        }

        public event Func<SocketUser, SocketUser, Task> UserUpdated;

        private async Task OnUserLeft(SocketGuildUser user)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserLeft)) return;
            await UserLeft(user);
        }

        public event Func<SocketUser, Task> UserLeft;

        private async Task OnUserJoined(SocketGuildUser user)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserJoined)) return;
            await UserJoined(user);
        }

        public event Func<SocketUser, Task> UserJoined;

        private async Task OnUserIsTyping(SocketUser user, ISocketMessageChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserIsTyping)) return;
            await UserIsTyping(user, channel);
        }

        public event Func<SocketUser, ISocketMessageChannel, Task> UserIsTyping;

        private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserUnbanned)) return;
            await UserUnbanned(user, guild);
        }

        public event Func<SocketUser, SocketGuild, Task> UserUnbanned;

        private async Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.UserBanned)) return;
            await UserBanned(user, guild);
        }

        public event Func<SocketUser, SocketGuild, Task> UserBanned;

        private async Task OnShardReady(DiscordSocketClient client)
        {
            if (await GetPreEventTaskResult(ClientEventType.ShardReady)) return;
            await ShardReady(client);
        }

        public event Func<DiscordSocketClient, Task> ShardReady;

        private async Task OnShardLatencyUpdated(int before, int after, DiscordSocketClient client)
        {
            if (await GetPreEventTaskResult(ClientEventType.ShardLatencyUpdated)) return;
            await ShardLatencyUpdated(before, after, client);
        }

        public event Func<int, int, DiscordSocketClient, Task> ShardLatencyUpdated;

        private async Task OnShardDisconnected(Exception exception, DiscordSocketClient client)
        {
            if (await GetPreEventTaskResult(ClientEventType.ShardDisconnected)) return;
            await ShardDisconnected(exception, client);
        }

        public event Func<Exception, DiscordSocketClient, Task> ShardDisconnected;

        private async Task OnShardConnected(DiscordSocketClient client)
        {
            if (await GetPreEventTaskResult(ClientEventType.ShardConnected)) return;
            await ShardConnected(client);
        }

        public event Func<DiscordSocketClient, Task> ShardConnected;

        private async Task OnRoleUpdated(SocketRole roleBefore, SocketRole roleAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.RoleUpdated)) return;
            await RoleUpdated(roleBefore, roleAfter);
        }

        public event Func<SocketRole, SocketRole, Task> RoleUpdated;

        private async Task OnRoleDeleted(SocketRole role)
        {
            if (await GetPreEventTaskResult(ClientEventType.RoleDeleted)) return;
            await RoleDeleted(role);
        }

        public event Func<SocketRole, Task> RoleDeleted;

        private async Task OnRoleCreated(SocketRole role)
        {
            if (await GetPreEventTaskResult(ClientEventType.RoleCreated)) return;
            await RoleCreated(role);
        }

        public event Func<SocketRole, Task> RoleCreated;

        private async Task OnRecipientRemoved(SocketGroupUser user)
        {
            if (await GetPreEventTaskResult(ClientEventType.RecipientRemoved)) return;
            await RecipientRemoved(user);
        }

        public event Func<SocketGroupUser, Task> RecipientRemoved;

        private async Task OnRecipientAdded(SocketGroupUser user)
        {
            if (await GetPreEventTaskResult(ClientEventType.RecipientAdded)) return;
            await RecipientAdded(user);
        }

        public event Func<SocketGroupUser, Task> RecipientAdded;

        private async Task OnReactionsCleared(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.ReactionsCleared)) return;
            await ReactionsCleared(cacheableMessage, channel);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task> ReactionsCleared;

        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (await GetPreEventTaskResult(ClientEventType.ReactionRemoved)) return;
            await ReactionRemoved(cacheableMessage, channel, reaction);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionRemoved;

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (await GetPreEventTaskResult(ClientEventType.ReactionAdded)) return;
            await ReactionAdded(cacheableMessage, channel, reaction);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAdded;

        private async Task OnMessageUpdated(Cacheable<IMessage, ulong> cacheableBefore, SocketMessage message, ISocketMessageChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.MessageUpdated)) return;
            await MessageUpdated(cacheableBefore, message, channel);
        }

        public event Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> MessageUpdated;

        private async Task OnMessageReceived(SocketMessage message)
        {
            if (await GetPreEventTaskResult(ClientEventType.MessageReceived)) return;
            await MessageReceived(message);
        }

        public event Func<SocketMessage, Task> MessageReceived;

        private async Task OnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.MessageDeleted)) return;
            await MessageDeleted(message, channel);
        }

        public event Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task> MessageDeleted;

        private async Task OnLoggedOut()
        {
            if (await GetPreEventTaskResult(ClientEventType.LoggedOut)) return;
            await LoggedOut();
        }

        public event Func<Task> LoggedOut;

        private async Task OnLoggedIn()
        {
            if (await GetPreEventTaskResult(ClientEventType.LoggedIn)) return;
            await LoggedIn();
        }

        public event Func<Task> LoggedIn;

        private async Task OnLog(LogMessage message)
        {
            if (await GetPreEventTaskResult(ClientEventType.Log)) return;
            await Log(message);
        }

        public event Func<LogMessage, Task> Log;

        private async Task OnLeftGuild(SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.LeftGuild)) return;
            await LeftGuild(guild);
        }

        public event Func<SocketGuild, Task> LeftGuild;

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.JoinedGuild)) return;
            await JoinedGuild(guild);
        }

        public event Func<SocketGuild, Task> JoinedGuild;

        public DiscordShardedClient Client { get; }

        private async Task OnGuildUpdated(SocketGuild guildBefore, SocketGuild guildAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.GuildUpdated)) return;
            await GuildUpdated(guildBefore, guildAfter);
        }

        public event Func<SocketGuild, SocketGuild, Task> GuildUpdated;

        private async Task OnGuildUnavailable(SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.GuildUnavailable)) return;
            await GuildUnavailable(guild);
        }

        public event Func<SocketGuild, Task> GuildUnavailable;

        private async Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.GuildMembersDownloaded)) return;
            await GuildMembersDownloaded(guild);
        }

        public event Func<SocketGuild, Task> GuildMembersDownloaded;

        private async Task OnGuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.GuildMemberUpdated)) return;
            await GuildMemberUpdated(userBefore, userAfter);
        }

        public event Func<SocketGuildUser, SocketGuildUser, Task> GuildMemberUpdated;

        private async Task OnGuildAvailable(SocketGuild guild)
        {
            if (await GetPreEventTaskResult(ClientEventType.GuildAvailable)) return;
            await GuildAvailable(guild);
        }

        public event Func<SocketGuild, Task> GuildAvailable;

        private async Task OnCurrentUserUpdated(SocketSelfUser userBefore, SocketSelfUser userAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.CurrentUserUpdated)) return;
            await CurrentUserUpdated(userBefore, userAfter);
        }

        public event Func<SocketSelfUser, SocketSelfUser, Task> CurrentUserUpdated;

        private async Task OnChannelCreated(SocketChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.ChannelCreated)) return;
            await ChannelCreated(channel);
        }

        public event Func<SocketChannel, Task> ChannelCreated;

        private async Task OnChannelDestroyed(SocketChannel channel)
        {
            if (await GetPreEventTaskResult(ClientEventType.ChannelDestroyed)) return;
            await ChannelDestroyed(channel);
        }

        public event Func<SocketChannel, Task> ChannelDestroyed;

        private async Task OnChannelUpdated(SocketChannel channelBefore, SocketChannel channelAfter)
        {
            if (await GetPreEventTaskResult(ClientEventType.ChannelUpdated)) return;
            await ChannelUpdated(channelBefore, channelAfter);
        }

        public event Func<SocketChannel, SocketChannel, Task> ChannelUpdated;
    }
}