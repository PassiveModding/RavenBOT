using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Core.TypeReaders.EmojiReader;

namespace RavenBOT.Handlers
{
    //Login setup and logging events
    public partial class EventHandler
    {
        internal DiscordShardedClient Client { get; }
        public PrefixService PrefixService { get; }
        internal BotConfig BotConfig { get; }
        internal LogHandler Logger { get; }
        internal CommandService CommandService { get; }
        public LocalManagementService LocalManagementService { get; }
        internal IServiceProvider Provider { get; }

        internal ModuleManagementService ModuleManager { get; }

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
            //commandService.CommandExecuted += async (cI, c, r) => await CommandExecutedAsync(cI, c, r);
        }

        internal async Task JoinedGuildAsync(SocketGuild guild)
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

        internal async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                Logger.Log(context.Message.Content, new LogContext(context));
            }
            else
            {
                Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", new LogContext(context), LogSeverity.Error);

                if (result is ExecuteResult exResult)
                {
                    await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                    {
                        Title = $"Command Execution Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                            Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                            "__**Error**__\n" +
                            $"{result.ErrorReason.FixLength(512)}\n" +
                            $"{exResult.Exception}".FixLength(1024),
                            Color = Color.LightOrange
                    }.Build());
                }
                else if (result is PreconditionResult preResult)
                { 
                    await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                    {
                        Title = $"Command Precondition Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                            Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                            "__**Error**__\n" +
                            $"{result.ErrorReason.FixLength(512)}\n".FixLength(1024),
                            Color = Color.LightOrange
                    }.Build());
                }
                else if (result is RuntimeResult runResult)
                {
                    //Post execution result. Ie. returned by developer
                    await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                    {
                        Title = $"Command Runtime Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                            Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                            "__**Error**__\n" +
                            $"{runResult.Reason.FixLength(512)}\n".FixLength(1024),
                            Color = Color.LightOrange
                    }.Build());
                }
                else if (result is SearchResult sResult)
                {
                    //Since it is an error you can assume it's an unknown command as SearchResults will only return an error if not found.
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
                }
                else if (result is ParseResult pResult)
                {
                    //Invalid parese result can be
                    //ParseFailed, "There must be at least one character of whitespace between arguments."
                    //ParseFailed, "Input text may not end on an incomplete escape."
                    //ParseFailed, "A quoted parameter is incomplete."
                    //BadArgCount, "The input text has too few parameters."
                    //BadArgCount, "The input text has too many parameters."
                    //typeReaderResults
                    if (pResult.Error.Value == CommandError.BadArgCount && commandInfo.IsSpecified)
                    {
                        await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                        {
                            Title = $"Argument Error {result.Error.Value}",
                                Description = $"`{commandInfo.Value.Aliases.First()}{string.Join(" ", commandInfo.Value.Parameters.Select(x => x.ParameterInformation()))}`\n" +
                                $"Message: {context.Message.Content.FixLength(512)}\n" +
                                "__**Error**__\n" +
                                $"{result.ErrorReason.FixLength(512)}",
                                Color = Color.DarkRed

                        }.Build());
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                        {
                            Title = $"Command Parse Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                                Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                                "__**Error**__\n" +
                                $"{result.ErrorReason.FixLength(512)}\n".FixLength(1024),
                                Color = Color.LightOrange
                        }.Build());
                    }
                }
                else
                {
                    await context.Channel.SendMessageAsync("", false, new EmbedBuilder
                    {
                        Title = $"Command Error{(result.Error.HasValue ? $": {result.Error.Value}" : "")}",
                            Description = $"Message: {context.Message.Content.FixLength(512)}\n" +
                            "__**Error**__\n" +
                            $"{result.ErrorReason.FixLength(512)}\n".FixLength(1024),
                            Color = Color.LightOrange
                    }.Build());
                }
            }
        }

        public async Task InitializeAsync()
        {
            CommandService.AddTypeReader(typeof(Emoji), new EmojiTypeReader());

            await Client.LoginAsync(TokenType.Bot, BotConfig.Token);
            await Client.StartAsync();
            await RegisterModulesAsync();
            var preconditionWarnings = new List<string>();
            foreach (var command in  CommandService.Commands)
            {
                foreach (var precondition in command.Preconditions)
                {
                    if (!(precondition is PreconditionBase preBase))
                    {
                        preconditionWarnings.Add($"CMD: {command.Aliases.First()} - {precondition}");
                    }
                }
            }

            foreach (var module in CommandService.Modules)
            {
                foreach (var precondition in module.Preconditions)
                {
                    if (!(precondition is PreconditionBase preBase))
                    {
                        preconditionWarnings.Add($"MDL: {module.Aliases.First()} - {precondition}");
                    }
                }
            }

            if (preconditionWarnings.Any())
            {
                var warnString = "The following commands/modules have preconditions that do not " + 
                                 $"inherit {typeof(PreconditionBase)} and will not display in the help commands\n" +
                                 string.Join("\n", preconditionWarnings);
                Logger.Log(warnString, LogSeverity.Warning);
            }
        }

        public async Task RegisterModulesAsync()
        {
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);
        }

        internal Task ShardConnectedAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} connected! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        internal Task ShardReadyAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} ready! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        internal Task LogAsync(LogMessage message)
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