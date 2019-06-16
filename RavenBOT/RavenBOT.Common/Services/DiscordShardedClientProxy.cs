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
            //TODO: Finish adding events
        }

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