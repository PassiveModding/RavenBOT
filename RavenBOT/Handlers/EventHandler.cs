namespace RavenBOT.Handlers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.Commands;

    using global::Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    using RavenBOT.Discord.Context;
    using RavenBOT.Models;

    /// <summary>
    /// The event handler.
    /// </summary>
    public class EventHandler
    {
        /// <summary>
        /// true = check and update all missing servers on start.
        /// </summary>
        private bool guildCheck = true;

        /// <summary>
        /// Displays bot invite on connection Once then gets toggled off.
        /// </summary>
        private bool hideInvite;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="config">
        /// The config.
        /// </param>
        /// <param name="dbConfig">
        /// The db Config.
        /// </param>
        /// <param name="service">
        /// The service.
        /// </param>
        /// <param name="commandService">
        /// The command service.
        /// </param>
        public EventHandler(DiscordShardedClient client, ConfigModel config, IServiceProvider service, CommandService commandService)
        {
            Client = client;
            Config = config;
            Provider = service;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the config.
        /// </summary>
        private ConfigModel Config { get; }

        /// <summary>
        /// Gets the db config.
        /// </summary>
        private DatabaseObject DBConfig { get; set; }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        private IServiceProvider Provider { get; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        private DiscordShardedClient Client { get; }

        /// <summary>
        /// Gets the command service.
        /// </summary>
        private CommandService CommandService { get; }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// The initialize async.
        /// </summary>
        /// <param name="dbConfig">
        /// The db Config.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitializeAsync(DatabaseObject dbConfig)
        {
            // This will add all our modules to the command service, allowing them to be accessed as necessary
            DBConfig = dbConfig;
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly());
            LogHandler.LogMessage("RavenBOT: Modules Added");
        }

        /// <summary>
        /// Triggers when a shard is ready
        /// </summary>
        /// <param name="socketClient">
        /// The socketClient.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task ShardReadyAsync(DiscordSocketClient socketClient)
        {
            await socketClient.SetActivityAsync(new Game($"Shard: {socketClient.ShardId}"));

            if (guildCheck)
            {
                // If all shards are connected, try to remove all guilds that no longer use the bot
                if (Client.Shards.All(x => x.ConnectionState == ConnectionState.Connected))
                {
                    if (DBConfig.Local.Developing)
                    {
                        LogHandler.LogMessage("Bot is in Developer Mode!", LogSeverity.Warning);
                    }

                    if (DBConfig.Local.Developing && DBConfig.Local.PrefixOverride != null)
                    {
                        LogHandler.LogMessage("Bot is in Prefix Override Mode!", LogSeverity.Warning);
                    }


                    _ = Task.Run(
                        () =>
                            {
                                var handler = Provider.GetRequiredService<DatabaseHandler>();

                                // Returns all stored guild models
                                var guildIds = Client.Guilds.Select(g => g.Id).ToList();
                                var missingList = handler.Query<GuildModel>().Select(x => x.ID).Where(x => !guildIds.Contains(x)).ToList();

                                foreach (var id in missingList)
                                {
                                    handler.Execute<GuildModel>(DatabaseHandler.Operation.DELETE, id: id.ToString());
                                }
                            });

                    // Ensure that this is only run once as the bot initially connects.
                    guildCheck = false;
                }
                else
                {
                    // This will check to ensure that all our servers are initialized, whilst also allowing the bot to continue starting
                    _ = Task.Run(
                        () =>
                            {
                                var handler = Provider.GetRequiredService<DatabaseHandler>();

                                // This will load all guild models and retrieve their IDs
                                var Servers = handler.Query<GuildModel>().Select(x => x.ID).ToList();

                                // Now if the bots server list contains a guild but 'Servers' does not, we create a new object for the guild
                                foreach (var Guild in socketClient.Guilds.Select(x => x.Id).Where(x => !Servers.Contains(x)))
                                {
                                    handler.Execute<GuildModel>(DatabaseHandler.Operation.CREATE, new GuildModel { ID = Guild }, Guild);
                                }
                            });
                }

                LogHandler.LogMessage($"Shard: {socketClient.ShardId} Ready");
                if (!hideInvite)
                {
                    LogHandler.LogMessage($"Invite: https://discordapp.com/oauth2/authorize?client_id={Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
                    hideInvite = true;
                }
            }
        }

        /// <summary>
        /// Triggers when a shard connects.
        /// </summary>
        /// <param name="socketClient">
        /// The Client.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task ShardConnectedAsync(DiscordSocketClient socketClient)
        {
            Task.Run(()
                => CancellationToken.Cancel()).ContinueWith(x
                => CancellationToken = new CancellationTokenSource());
            LogHandler.LogMessage($"Shard: {socketClient.ShardId} Connected with {socketClient.Guilds.Count} Guilds and {socketClient.Guilds.Sum(x => x.MemberCount)} Users");
            return Task.CompletedTask;
        }

        /// <summary>
        /// This logs discord messages to our LogHandler
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task LogAsync(LogMessage message)
        {
            return Task.Run(() => LogHandler.LogMessage(message.Message, message.Severity));
        }
        
        /// <summary>
        /// This will auto-remove the bot from servers as it gets removed. NOTE: Remove this if you want to save configs.
        /// </summary>
        /// <param name="guild">
        /// The guild.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task LeftGuildAsync(SocketGuild guild)
        {
            return Task.Run(()
                => Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.DELETE, id: guild.Id));
        }

        /// <summary>
        /// This will automatically initialize any new guilds for the bot.
        /// </summary>
        /// <param name="guild">
        /// The guild.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task JoinedGuildAsync(SocketGuild guild)
        {
            return Task.Run(()=>
            {
                var handler = Provider.GetRequiredService<DatabaseHandler>();
                if (handler.Execute<GuildModel>(DatabaseHandler.Operation.LOAD, id: guild.Id) == null)
                {
                    handler.Execute<GuildModel>(DatabaseHandler.Operation.CREATE, new GuildModel { ID = guild.Id }, guild.Id);
                }
            });
        }

        /// <summary>
        /// This event is triggered every time the a user sends a message in a channel, dm etc. that the bot has access to view.
        /// </summary>
        /// <param name="socketMessage">
        /// The socket message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage Message) || Message.Channel is IDMChannel)
            {
                return;
            }

            var context = new Context(Client, Message, Provider);

            if (Config.LogUserMessages)
            {
                LogHandler.LogMessage(context);
            }

            var argPos = 0;

            if (DBConfig.Local.Developing && DBConfig.Local.PrefixOverride != null)
            {
                if (!Message.HasStringPrefix(DBConfig.Local.PrefixOverride, ref argPos))
                {
                    return;
                }
            }
            else
            {
                // Filter out all messages that don't start with our Bot Prefix, bot mention or server specific prefix.
                if (!(Message.HasStringPrefix(Config.Prefix, ref argPos) || Message.HasMentionPrefix(context.Client.CurrentUser, ref argPos) || Message.HasStringPrefix(context.Server.Settings.CustomPrefix, ref argPos)))
                {
                    return;
                }
            }


            // Here we attempt to execute a command based on the user message
            var result = await CommandService.ExecuteAsync(context, argPos, Provider, MultiMatchHandling.Best);

            // Generate an error message for users if a command is unsuccessful
            if (!result.IsSuccess)
            {
                var _ = Task.Run(() => CmdErrorAsync(context, result, argPos));
            }
            else
            {
                if (Config.LogCommandUsages)
                {
                    LogHandler.LogMessage(context);
                }
            }
        }

        /// <summary>
        /// Generates an error message based on a command error.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="argPos">
        /// The arg pos.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task CmdErrorAsync(Context context, IResult result, int argPos)
        {
            string errorMessage;
            if (result.Error == CommandError.UnknownCommand)
            {
                errorMessage = "**Command:** N/A";
            }
            else
            {
                // Search the commandservice based on the message, then respond accordingly with information about the command.
                var search = CommandService.Search(context, argPos);
                var cmd = search.Commands.FirstOrDefault();
                errorMessage = $"**Command Name:** `{cmd.Command.Name}`\n" +
                               $"**Summary:** `{cmd.Command?.Summary ?? "N/A"}`\n" +
                               $"**Remarks:** `{cmd.Command?.Remarks ?? "N/A"}`\n" +
                               $"**Aliases:** {(cmd.Command.Aliases.Any() ? string.Join(" ", cmd.Command.Aliases.Select(x => $"`{x}`")) : "N/A")}\n" +
                               $"**Parameters:** {(cmd.Command.Parameters.Any() ? string.Join(" ", cmd.Command.Parameters.Select(x => x.IsOptional ? $" `<(Optional){x.Name}>` " : $" `<{x.Name}>` ")) : "N/A")}\n" +
                               "**Error Reason**\n" +
                               $"{result.ErrorReason}";
            }

            try
            {
                await context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder
                {
                    Title = "ERROR",
                    Description = errorMessage
                }.Build());
            }
            catch
            {
                // ignored
            }

            await LogErrorAsync(result, context);
        }

        /// <summary>
        /// Logs specified errors based on type.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task LogErrorAsync(IResult result, Context context)
        {
            switch (result.Error)
            {
                case CommandError.MultipleMatches:
                    if (Config.LogCommandUsages)
                    {
                        LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
                    }

                    break;
                case CommandError.ObjectNotFound:
                    if (Config.LogCommandUsages)
                    {
                        LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
                    }

                    break;
                case CommandError.Unsuccessful:
                    await context.Channel.SendMessageAsync("You may have found a bug. Please report this error in my server https://discord.me/Passive");
                    break;
            }
        }
    }
}