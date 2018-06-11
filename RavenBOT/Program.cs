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
                    .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                ;

            var Provider = Services.BuildServiceProvider();
            Provider.GetRequiredService<DatabaseHandler>().Initialize();
            var shards = Provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, Id: "Config").shards;
            Services.AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 20,
                AlwaysDownloadUsers = true,
                LogLevel = LogSeverity.Warning,
                //Please change increase this as your server count grows beyond 2000 guilds. ie. < 2000 = 1, 2000 = 2, 4000 = 2 ...
                TotalShards = shards
            }));
            Provider = Services.BuildServiceProvider();
            await Provider.GetRequiredService<BotHandler>().InitializeAsync();
            await Provider.GetRequiredService<EventHandler>().InitializeAsync();

            await Task.Delay(-1);
        }
    }
}