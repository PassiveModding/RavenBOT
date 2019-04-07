using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Models;
using RavenBOT.Services;

namespace RavenBOT.Handlers
{
    //Login setup and logging events
    public partial class EventHandler
    {
        private DiscordShardedClient Client { get; }
        public PrefixService PrefixService { get; }
        private DatabaseService DbService { get; }
        private BotConfig BotConfig { get; }
        private LogHandler Logger { get; }
        public GraphiteService GraphiteService { get; }
        private CommandService CommandService { get; }
        private IServiceProvider Provider { get; }


        public EventHandler(DiscordShardedClient client, PrefixService prefixService, DatabaseService dbService, CommandService commandService, BotConfig config, LogHandler handler, GraphiteService graphiteService, IServiceProvider provider)
        {
            Client = client;
            PrefixService = prefixService;
            DbService = dbService;
            Logger = handler;
            GraphiteService = graphiteService;
            BotConfig = config;
            CommandService = commandService;
            Provider = provider;
            client.Log += LogAsync;
            client.ShardReady += ShardReadyAsync;
            client.ShardConnected += ShardConnectedAsync;
            client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            await Client.LoginAsync(TokenType.Bot, BotConfig.Token);
            await Client.StartAsync();
            await RegisterModulesAsync();
        }

        public Task RegisterModulesAsync() => CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);

        private Task ShardConnectedAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} connected! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        private Task ShardReadyAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} ready! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException exc)
            {
                Logger.Log(message.Message, new LogContext(exc.Context), message.Severity);
                return Task.CompletedTask;
            }

            Logger.Log(message.Message, message.Severity);
            return Task.CompletedTask;
        }
    }
}
