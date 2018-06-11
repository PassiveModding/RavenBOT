using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;
using EventHandler = RavenBOT.Handlers.EventHandler;
using InteractiveService = RavenBOT.Discord.Context.Interactive;

namespace RavenBOT
{
    public class Program
    {
        public static DiscordShardedClient Client;

        public static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        private static async Task Start()
        {
            //This ensures that our bot's setup directory is initialised and will be were the database config is stored.
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));

            var Services = new ServiceCollection()
                    .AddSingleton<DatabaseHandler>()
                    .AddSingleton(x => x.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, Id: "Config"))
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        ThrowOnError = false,
                        IgnoreExtraArgs = false,
                        DefaultRunMode = RunMode.Async
                    }))
                    .AddSingleton<BotHandler>()
                    .AddSingleton<EventHandler>()
                    .AddSingleton<InteractiveService>()
                    .AddSingleton(new Random(Guid.NewGuid().GetHashCode()));


            var Provider = Services.BuildServiceProvider();

            //This method ensures that the database is
            //1. Running
            //2. Set up properly 
            //3. contains the bot config itself
            Provider.GetRequiredService<DatabaseHandler>().Initialize();
            //The provider is split here so we can get our shard count from the database before actually logging into discord.
            //This is important to do so the bot always logs in with the required amount of shards.
            var shards = Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, Id: "Config").shards;
            Services.AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 20,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Warning,
                //Please change increase this as your server count grows beyond 2000 guilds. ie. < 2000 = 1, 2000 = 2, 4000 = 2 ...
                TotalShards = shards
            }));

            //Build the service provider a second time so that the ShardedClient is now included.
            Provider = Services.BuildServiceProvider();

            await Provider.GetRequiredService<BotHandler>().InitializeAsync();
            await Provider.GetRequiredService<EventHandler>().InitializeAsync();

            //Indefinitely delay the method from finishing so that the program stays running until stopped.
            await Task.Delay(-1);
        }
    }
}