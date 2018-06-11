﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Discord.Context;
using RavenBOT.Models;

namespace RavenBOT.Handlers
{
    public class EventHandler
    {
        private DatabaseHandler DB { get; }
        private Random Random { get; }
        private ConfigModel Config { get; }
        private IServiceProvider Provider { get; }
        private DiscordSocketClient Client { get; }
        private bool GuildCheck = true;
        private CommandService CommandService { get; }
        private CancellationTokenSource CancellationToken { get; set; }
        public EventHandler(DatabaseHandler db, DiscordSocketClient client, ConfigModel config, IServiceProvider service, CommandService commandService, Random random)
        {
            DB = db;
            Client = client;
            Config = config;
            Provider = service;
            Random = random;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        public async Task InitializeAsync()
        {
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly());
            LogHandler.LogMessage("RavenBOT: Modules Added");
        }

        internal async Task Ready()
        {
            //Here we select at random out 'playing' message.
            var Games = new Dictionary<ActivityType, string[]>
            {
                {ActivityType.Listening, new[]{"YT/PassiveModding", "Tech N9ne"} },
                {ActivityType.Playing, new[]{$"{Config.Prefix}help"} },
                {ActivityType.Watching, new []{"YT/PassiveModding"} }
            };
            var RandomActivity = Games.Keys.ToList()[Random.Next(Games.Keys.Count)];
            var RandomName = Games[RandomActivity][Random.Next(Games[RandomActivity].Length)];
            await Client.SetActivityAsync(new Game(RandomName, RandomActivity));
            LogHandler.LogMessage($"Game has been set to: [{RandomActivity}] {RandomName}");
            Games.Clear();

            
            if (GuildCheck)
            {
                //This will check to ensure that all our servers are initialised, whilst also allowing the bot to continue starting
                _ = Task.Run(() =>
                {
                    //This will load all guild models and reterive their IDs
                    var Servers = Provider.GetRequiredService<DatabaseHandler>().Query<GuildModel>().Select(x => Convert.ToUInt64(x.ID)).ToList();
                    //Now if the bot's server list contains a guild but 'Servers' does not, we create a new object for the Guild
                    foreach (var Guild in Client.Guilds.Select(x => x.Id))
                    {
                        if (!Servers.Contains(Guild))
                            Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.CREATE, new GuildModel
                            {
                                ID = Guild
                            }, Guild);
                    }

                    //We also auto-remove any servers that no longer use the bot, to reduce un-necessary disk usage. 
                    //You may want to remove this however if you are storing things and want to keep them.
                    foreach (var Server in Servers)
                    {
                        if (!Client.Guilds.Select(x => x.Id).Contains(Convert.ToUInt64(Server)))
                            Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.DELETE, Id: Server);
                    }

                    //Ensure that this is only run once as the bot initially connects.
                    GuildCheck = false;
                });
            }

            //This will log a message with the bot's invite link so the developer can access the bot's invite with ease. Note the permissions are configured to allow everything in the server.
            var application = Client.GetApplicationInfoAsync();
            LogHandler.LogMessage($"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
        }

        internal Task Connected()
            => Task.Run(()
                => CancellationToken.Cancel()).ContinueWith(x
                => CancellationToken = new CancellationTokenSource());


        internal Task Log(LogMessage Message)
            => Task.Run(() => LogHandler.LogMessage(Message.Message, Message.Severity));

        //This will auto-remove the bot from servers as it gets removed. NOTE: Remove this if you want to save configs.
        internal Task LeftGuild(SocketGuild Guild) => Task.Run(()
            => Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.DELETE, Id: Guild.Id));

        //This event is triggered every time the a user sends a message in a channel, dm etc. that the bot has access to view.
        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage Message) || Message.Channel is IDMChannel) return;
            var Context = new Context(Client, Message, Provider);

            var argPos = 0;
            //Filter out all messages that don't start with our Bot Prefix, bot mention or server specific prefix.
            if (!(Message.HasStringPrefix(Config.Prefix, ref argPos) || Message.HasMentionPrefix(Context.Client.CurrentUser, ref argPos) || Message.HasStringPrefix(Context.Server.Settings.CustomPrefix, ref argPos))) return;

            //Here we attempt to execute a command based on the user message
            var Result = await CommandService.ExecuteAsync(Context, argPos, Provider, MultiMatchHandling.Best);

            //Generate an error message for users if a command is unsuccessful
            if (!Result.IsSuccess)
            {
                string ErrorMessage;
                if (Result.Error == CommandError.UnknownCommand)
                {
                    ErrorMessage = "**Command:** N/A";
                }
                else
                {
                    var srch = CommandService.Search(Context, argPos);
                    var cmd = srch.Commands.FirstOrDefault();
                    ErrorMessage = $"**Command Name:** `{cmd.Command.Name}`\n" +
                                   $"**Summary:** `{cmd.Command?.Summary ?? "N/A"}`\n" +
                                   $"**Remarks:** `{cmd.Command?.Remarks ?? "N/A"}`\n" +
                                   $"**Aliases:** {(cmd.Command.Aliases.Any() ? string.Join(" ", cmd.Command.Aliases.Select(x => $"`{x}`")) : "N/A")}\n" +
                                   $"**Parameters:** {(cmd.Command.Parameters.Any() ? string.Join(" ", cmd.Command.Parameters.Select(x => x.IsOptional ? $" `<(Optional){x.Name}>` " : $" `<{x.Name}>` ")) : "N/A")}\n" +
                                   "**Error Reason**\n" +
                                   $"{Result.ErrorReason}";
                }

                try
                {
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder
                    {
                        Title = "ERROR",
                        Description = ErrorMessage
                    }.Build());
                }
                catch
                {
                    //
                }

                switch (Result.Error)
                {
                    case CommandError.UnmetPrecondition:
                        var Permissions = (Message.Channel as SocketGuildChannel).Guild.CurrentUser.GuildPermissions;
                        if (!string.IsNullOrWhiteSpace(Result.ErrorReason) && Permissions.SendMessages)
                            await Message.Channel.SendMessageAsync(Result.ErrorReason); break;
                    case CommandError.MultipleMatches: LogHandler.LogMessage(Result.ErrorReason, LogSeverity.Error); break;
                    case CommandError.ObjectNotFound: LogHandler.LogMessage(Result.ErrorReason, LogSeverity.Error); break;
                    case CommandError.Unsuccessful:
                        await Message.Channel.SendMessageAsync("You may have found a bug. Please report this error in my server https://discord.me/Passive");
                        break;
                }
            }
        }
    }
}