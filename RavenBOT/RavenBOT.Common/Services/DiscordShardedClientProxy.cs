using System;
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

        //TODO: Expose method of adding certain functions to run prior to each task.

        private Task OnVoiceServerUpdated(SocketVoiceServer server)
        {
            return VoiceServerUpdated(server);
        }

        public event Func<SocketVoiceServer, Task> VoiceServerUpdated;

        private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceBefore, SocketVoiceState voiceAfter)
        {
            return UserVoiceStateUpdated(user, voiceBefore, voiceAfter);
        }

        public event Func<SocketUser, SocketVoiceState, SocketVoiceState, Task> UserVoiceStateUpdated;

        private Task OnUserUpdated(SocketUser userBefore, SocketUser userAfter)
        {
            return UserUpdated(userBefore, userAfter);
        }

        public event Func<SocketUser, SocketUser, Task> UserUpdated;

        private Task OnUserLeft(SocketGuildUser user)
        {
            return UserLeft(user);
        }

        public event Func<SocketUser, Task> UserLeft;

        private Task OnUserJoined(SocketGuildUser user)
        {
            return UserJoined(user);
        }

        public event Func<SocketUser, Task> UserJoined;

        private Task OnUserIsTyping(SocketUser user, ISocketMessageChannel channel)
        {
            return UserIsTyping(user, channel);
        }

        public event Func<SocketUser, ISocketMessageChannel, Task> UserIsTyping;

        private Task OnUserUnbanned(SocketUser user, SocketGuild guild)
        {
            return UserUnbanned(user, guild);
        }

        public event Func<SocketUser, SocketGuild, Task> UserUnbanned;

        private Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            return UserBanned(user, guild);
        }

        public event Func<SocketUser, SocketGuild, Task> UserBanned;

        private Task OnShardReady(DiscordSocketClient client)
        {
            return ShardReady(client);
        }

        public event Func<DiscordSocketClient, Task> ShardReady;

        private Task OnShardLatencyUpdated(int before, int after, DiscordSocketClient client)
        {
            return ShardLatencyUpdated(before, after, client);
        }

        public event Func<int, int, DiscordSocketClient, Task> ShardLatencyUpdated;

        private Task OnShardDisconnected(Exception exception, DiscordSocketClient client)
        {
            return ShardDisconnected(exception, client);
        }

        public event Func<Exception, DiscordSocketClient, Task> ShardDisconnected;

        private Task OnShardConnected(DiscordSocketClient client)
        {
            return ShardConnected(client);
        }

        public event Func<DiscordSocketClient, Task> ShardConnected;

        private Task OnRoleUpdated(SocketRole roleBefore, SocketRole roleAfter)
        {
            return RoleUpdated(roleBefore, roleAfter);
        }

        public event Func<SocketRole, SocketRole, Task> RoleUpdated;

        private Task OnRoleDeleted(SocketRole role)
        {
            return RoleDeleted(role);
        }

        public event Func<SocketRole, Task> RoleDeleted;
        
        private Task OnRoleCreated(SocketRole role)
        {
            return RoleCreated(role);
        }

        public event Func<SocketRole, Task> RoleCreated;

        private Task OnRecipientRemoved(SocketGroupUser user)
        {
            return RecipientRemoved(user);
        }

        public event Func<SocketGroupUser, Task> RecipientRemoved;

        private Task OnRecipientAdded(SocketGroupUser user)
        {
            return RecipientAdded(user);
        }

        public event Func<SocketGroupUser, Task> RecipientAdded;
        
        private Task OnReactionsCleared(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel)
        {
            return ReactionsCleared(cacheableMessage, channel);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, Task> ReactionsCleared;

        private Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            return ReactionRemoved(cacheableMessage, channel, reaction);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionRemoved;

        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            return ReactionAdded(cacheableMessage, channel, reaction);
        }

        public event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAdded;

        private Task OnMessageUpdated(Cacheable<IMessage, ulong> cacheableBefore, SocketMessage message, ISocketMessageChannel channel)
        {
            return MessageUpdated(cacheableBefore, message, channel);
        }

        public event Func<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel, Task> MessageUpdated;

        private Task OnMessageReceived(SocketMessage message)
        {
            return MessageReceived(message);
        }

        public event Func<SocketMessage, Task> MessageReceived;
        
        private Task OnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            return MessageDeleted(message, channel);
        }

        public event Func<Cacheable<IMessage, ulong>, ISocketMessageChannel, Task> MessageDeleted;
        
        private Task OnLoggedOut()
        {
            return LoggedOut();
        }

        public event Func<Task> LoggedOut;

        private Task OnLoggedIn()
        {
            return LoggedIn();
        }

        public event Func<Task> LoggedIn;


        private Task OnLog(LogMessage message)
        {
            return Log(message);
        }

        public event Func<LogMessage, Task> Log;


        private Task OnLeftGuild(SocketGuild guild)
        {
            return LeftGuild(guild);
        }

        public event Func<SocketGuild, Task> LeftGuild;

        private Task OnJoinedGuild(SocketGuild guild)
        {
            return JoinedGuild(guild);
        }

        public event Func<SocketGuild, Task> JoinedGuild;

        public DiscordShardedClient Client { get; }

        private Task OnGuildUpdated(SocketGuild guildBefore, SocketGuild guildAfter)
        {
            return GuildUpdated(guildBefore, guildAfter);
        }

        public event Func<SocketGuild, SocketGuild, Task> GuildUpdated;

        private Task OnGuildUnavailable(SocketGuild guild)
        {
            return GuildUnavailable(guild);
        }

        public event Func<SocketGuild, Task> GuildUnavailable;

        private Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            return GuildMembersDownloaded(guild);
        }

        public event Func<SocketGuild, Task> GuildMembersDownloaded;

        private Task OnGuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            return GuildMemberUpdated(userBefore, userAfter);
        }

        public event Func<SocketGuildUser, SocketGuildUser, Task> GuildMemberUpdated;

        private Task OnGuildAvailable(SocketGuild guild)
        {
            return GuildAvailable(guild);
        }

        public event Func<SocketGuild, Task> GuildAvailable;

        private Task OnCurrentUserUpdated(SocketSelfUser userBefore, SocketSelfUser userAfter)
        {
            return CurrentUserUpdated(userBefore, userAfter);
        }

        public event Func<SocketSelfUser, SocketSelfUser, Task> CurrentUserUpdated;

        private Task OnChannelCreated(SocketChannel channel)
        {
            return ChannelCreated(channel);
        }

        public event Func<SocketChannel, Task> ChannelCreated;

        private Task OnChannelDestroyed(SocketChannel channel)
        {
            return ChannelDestroyed(channel);
        }

        public event Func<SocketChannel, Task> ChannelDestroyed;
                
        private Task OnChannelUpdated(SocketChannel channelBefore, SocketChannel channelAfter)
        {
            return ChannelUpdated(channelBefore, channelAfter);
        }

        public event Func<SocketChannel, SocketChannel, Task> ChannelUpdated;
    }
}