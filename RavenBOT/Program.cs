using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;
using EventHandler = RavenBOT.Handlers.EventHandler;

namespace RavenBOT
{
    public class Program
    {
        public DiscordSocketClient Client;

        public static void Main(string[] args) => Start().GetAwaiter().GetResult();

        private static async Task Start()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));

            var Services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 20,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Warning
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    ThrowOnError = false,
                    IgnoreExtraArgs = false,
                    DefaultRunMode = RunMode.Async
                }))
                .AddSingleton<DatabaseHandler>()
                .AddSingleton<BotHandler>()
                .AddSingleton<EventHandler>()
                .AddSingleton<InteractiveService>()
                .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                .AddSingleton(x => x.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, Id: "Config"));

            var Provider = Services.BuildServiceProvider();
            Provider.GetRequiredService<DatabaseHandler>().Initialize();
            await Provider.GetRequiredService<BotHandler>().InitializeAsync();
            await Provider.GetRequiredService<EventHandler>().InitializeAsync();

            await Task.Delay(-1);
        }
    }
}