using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.TypeReaders.EmojiReader;

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
        public LocalManagementService LocalManagementService { get; }
        private IServiceProvider Provider { get; }

        private ModuleManagementService ModuleManager { get; }

        public EventHandler(DiscordShardedClient client, ModuleManagementService moduleManager, PrefixService prefixService, CommandService commandService, LocalManagementService local, BotConfig config, LogHandler handler, IServiceProvider provider)
        {
            Client = client;
            PrefixService = prefixService;
            Logger = handler;
            BotConfig = config;
            CommandService = commandService;
            LocalManagementService = local;
            Provider = provider;
            ModuleManager = moduleManager;

            client.Log += LogAsync;
            //client.Log += async (m) => await LogAsync(m);
            client.ShardReady += ShardReadyAsync;
            client.ShardConnected += ShardConnectedAsync;
            client.MessageReceived += MessageReceivedAsync;
            commandService.CommandExecuted += CommandExecutedAsync;
            client.JoinedGuild += JoinedGuildAsync;
            ModulePrefixes = new List<string>();
            //commandService.CommandExecuted += async (cI, c, r) => await CommandExecutedAsync(cI, c, r);
        }

        private async Task JoinedGuildAsync(SocketGuild guild)
        {
            if (!LocalManagementService.LastConfig.IsAcceptable(guild.Id))
            {
                return;
            }

            var user = guild.GetUser(Client.CurrentUser.Id);
            if (user == null)
            {
                await guild.DownloadUsersAsync();
                user = guild.GetUser(Client.CurrentUser.Id);
            }

            var firstChannel = guild.TextChannels.Where(x =>
            {
                var permissions = user?.GetPermissions(x);
                return permissions.HasValue ? permissions.Value.ViewChannel && permissions.Value.SendMessages : false;
            }).OrderBy(c => c.Position).FirstOrDefault();

            var prefix = LocalManagementService.LastConfig.Developer ? LocalManagementService.LastConfig.DeveloperPrefix : PrefixService.GetPrefix(guild.Id);

            await firstChannel?.SendMessageAsync("", false, new EmbedBuilder()
            {
                Title = $"{Client.CurrentUser.Username}",
                    Description = $"Get started by using the help command: `{prefix}help`",
                    Color = Color.Green
            }.Build());
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                Logger.Log(context.Message.Content, new LogContext(context));
            }
            else
            {
                Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", new LogContext(context), LogSeverity.Error);
                if (result.Error.HasValue)
                {
                    if (result.Error.Value == CommandError.UnknownCommand)
                    {
                        var prefix = LocalManagementService.LastConfig.Developer ? LocalManagementService.LastConfig.DeveloperPrefix : PrefixService.GetPrefix(context.Guild?.Id ?? 0);
                        var stripped = context.Message.Content.Substring(prefix.Length);
                        var dlDistances = new List<Tuple<int, string>>();
                        foreach (var command in CommandService.Commands)
                        {
                            foreach (var alias in command.Aliases)
                            {
                                var distance = stripped.DamerauLavenshteinDistance(alias);
                                if (distance == stripped.Length || distance == alias.Length)
                                {
                                    continue;
                                }

                                dlDistances.Add(new Tuple<int, string>(distance, alias));
                            }
                        }

                        var similar = dlDistances.OrderBy(x => x.Item1).Take(5).Select(x => prefix + x.Item2).ToList();
                        await context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Title = $"Unknown Command",
                                Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                                $"Similar commands: \n{string.Join("\n", similar)}",
                                Color = Color.Red
                        }.Build());
                        return;
                    }
                    else if (result.Error.Value == CommandError.BadArgCount && commandInfo.IsSpecified)
                    {
                        await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                        {
                            Title = $"Command Error {result.Error.Value}",
                                Description = $"`{commandInfo.Value.Aliases.First()}{string.Join(" ", commandInfo.Value.Parameters.Select(x => x.ParameterInformation()))}`\n" +
                                $"Message: {context.Message.Content.FixLength(512)}\n" +
                                "__**Error**__\n" +
                                $"{result.ErrorReason.FixLength(512)}",
                                Color = Color.DarkRed

                        }.Build());
                        return;
                    }
                }

                await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                {
                    Title = $"Command Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                        Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                        "__**Error**__\n" +
                        $"{result.ErrorReason.FixLength(512)}",
                        Color = Color.LightOrange
                }.Build());
            }
        }

        public async Task InitializeAsync()
        {
            CommandService.AddTypeReader(typeof(Emoji), new EmojiTypeReader());

            await Client.LoginAsync(TokenType.Bot, BotConfig.Token);
            await Client.StartAsync();
            await RegisterModulesAsync();
            ModulePrefixes = CommandService.Modules.Select(x => x.Group ?? "").Distinct().ToList();
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
            if (message.Message?.Contains("Rate limit triggered", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return Task.CompletedTask;
            }

            if (message.Exception is Exception e)
            {
                Logger.Log($"{message.Message}\n{e}", message.Severity);
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