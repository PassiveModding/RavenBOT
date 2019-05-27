using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Services;

namespace RavenBOT.Handlers
{
    //Login setup and logging events
    public partial class EventHandler
    {
        private DiscordShardedClient Client { get; }
        public PrefixService PrefixService { get; }
        private BotConfig BotConfig { get; }
        private LogHandler Logger { get; }
        private CommandService CommandService { get; }
        public LocalManagementService.LocalConfig Local { get; }
        private IServiceProvider Provider { get; }

        private ModuleManagementService ModuleManager {get;}

        public EventHandler(DiscordShardedClient client, ModuleManagementService moduleManager, PrefixService prefixService, CommandService commandService, LocalManagementService local, BotConfig config, LogHandler handler, IServiceProvider provider)
        {
            Client = client;
            PrefixService = prefixService;
            Logger = handler;
            BotConfig = config;
            CommandService = commandService;
            Local = local.GetConfig();
            Provider = provider;
            ModuleManager = moduleManager;

            client.Log += LogAsync;
            //client.Log += async (m) => await LogAsync(m);
            client.ShardReady += ShardReadyAsync;
            client.ShardConnected += ShardConnectedAsync;
            client.MessageReceived += MessageReceivedAsync;
            commandService.CommandExecuted += CommandExecutedAsync;
            ModulePrefixes = new List<string>();
            //commandService.CommandExecuted += async (cI, c, r) => await CommandExecutedAsync(cI, c, r);
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (commandInfo.IsSpecified)
            {
                if (commandInfo.Value.RunMode == RunMode.Sync)
                {
                    return;
                }
            }

            if (result.IsSuccess)
            {
                Logger.Log(context.Message.Content, new LogContext(context));
            }
            else
            {
                Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", new LogContext(context), LogSeverity.Error);
                await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                {
                    Title = $"Command Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                    Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                                  "__**Error**__\n" +
                                  $"{result.ErrorReason.FixLength(512)}"

                }.Build());
            }
        }

        public async Task InitializeAsync()
        {
            await Client.LoginAsync(TokenType.Bot, BotConfig.Token);
            await Client.StartAsync();
            await RegisterModulesAsync();
            if (!BotConfig.UsePrefixSystem)
            {
                if (CommandService.Modules.Any(x => string.IsNullOrWhiteSpace(x.Group)))
                {
                    Logger.Log("Some modules do not have groups assigned, this can cause unintended issues if you are not using a default prefix for the bot.", LogSeverity.Warning);
                }
                
                ModulePrefixes = CommandService.Modules.Select(x => x.Group ?? "").Distinct().ToList();
            }
        }

        public async Task RegisterModulesAsync() 
        {
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);
        }
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
            if (message.Message.Contains("Rate limit triggered", StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.CompletedTask;
            }

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
