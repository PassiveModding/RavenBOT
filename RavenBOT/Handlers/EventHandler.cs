﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Services;
using Sparrow.Platform.Posix.macOS;

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
            //client.Log += async (m) => await LogAsync(m);
            client.ShardReady += ShardReadyAsync;
            client.ShardConnected += ShardConnectedAsync;
            client.MessageReceived += MessageReceivedAsync;
            commandService.CommandExecuted += CommandExecutedAsync;
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

        private async Task LogAsync(LogMessage message)
        {
            if (message.Message.Contains("Rate limit triggered", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (message.Exception is CommandException exc)
            {
                Logger.Log(message.Message, new LogContext(exc.Context), message.Severity);
                return;
            }
            
            Logger.Log(message.Message, message.Severity);
        }
    }
}
