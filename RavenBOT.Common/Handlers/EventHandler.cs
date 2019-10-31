using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Common.TypeReaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RavenBOT.Common
{
    //Login setup and logging events
    public abstract class EventHandler
    {
        public IServiceProvider Provider { get; }
        public DiscordShardedClient Client { get; }
        public LogHandler Logger { get; }
        public CommandService CommandService { get; }
        public ShardChecker ShardChecker { get; }

        public EventHandler(IServiceProvider provider)
        {
            Provider = provider;
            Client = provider.GetRequiredService<DiscordShardedClient>();
            Logger = provider.GetService<LogHandler>() ?? new LogHandler();
            CommandService = provider.GetService<CommandService>() ?? new CommandService();
            ShardChecker = provider.GetService<ShardChecker>() ?? new ShardChecker(Client);
            ShardChecker.AllShardsReady += AllShardsReadyAsync;
            Client.ShardConnected += ShardConnectedAsync;
            Client.ShardReady += ShardReadyAsync;
        }

        public virtual Task AllShardsReadyAsync()
        {
            Client.MessageReceived += MessageReceivedAsync;
            Client.JoinedGuild += JoinedGuildAsync;
            Logger.Log("All shards ready, message received and joined guild events are now subscribed.");
            return Task.CompletedTask;
        }

        public abstract Task MessageReceivedAsync(SocketMessage discordMessage);

        public abstract Task JoinedGuildAsync(SocketGuild guild);

        public virtual async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                Logger.Log(context.Message.Content, context);
            }
            else
            {
                if (result is ExecuteResult exResult)
                {
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}\n{exResult.Exception}", context, LogSeverity.Error);
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
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", context, LogSeverity.Error);
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
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", context, LogSeverity.Error);
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
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", context, LogSeverity.Error);

                    //Since it is an error you can assume it's an unknown command as SearchResults will only return an error if not found.
                    var dlDistances = new List<Tuple<int, string>>();
                    foreach (var command in CommandService.Commands)
                    {
                        foreach (var alias in command.Aliases)
                        {
                            var distance = context.Message.Content.DamerauLavenshteinDistance(alias);
                            if (distance == context.Message.Content.Length || distance == alias.Length)
                            {
                                continue;
                            }

                            dlDistances.Add(new Tuple<int, string>(distance, alias));
                        }
                    }

                    var similar = dlDistances.OrderBy(x => x.Item1).Take(5).Select(x => x.Item2).ToList();
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
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", context, LogSeverity.Error);
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
                    Logger.Log($"{context.Message.Content}\n{result.Error}\n{result.ErrorReason}", context, LogSeverity.Error);
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

        /// <summary>
        /// Adds the emoji typereader, logs the client in, registers modules and hooks some events.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task InitializeAsync(string token)
        {
            CommandService.AddTypeReader(typeof(Emoji), new EmojiTypeReader());

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            await RegisterModulesAsync();
            CommandService.CommandExecuted += CommandExecutedAsync;
            CommandService.Log += LogAsync;
        }

        public virtual Task RegisterModulesAsync()
        {
            return CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);
        }

        public virtual Task ShardConnectedAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} connected! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        public virtual Task ShardReadyAsync(DiscordSocketClient shard)
        {
            Logger.Log($"Shard {shard.ShardId} ready! Guilds:{shard.Guilds.Count} Users:{shard.Guilds.Sum(x => x.MemberCount)}");
            return Task.CompletedTask;
        }

        public virtual Task LogAsync(LogMessage message)
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
                Logger.Log(message.Message, exc.Context, message.Severity);
                return Task.CompletedTask;
            }

            Logger.Log(message.Message, message.Severity);
            return Task.CompletedTask;
        }
    }
}