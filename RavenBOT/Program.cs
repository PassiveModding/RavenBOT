using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.Services.Licensing;
using EventHandler = RavenBOT.Handlers.EventHandler;

namespace RavenBOT
{
    public class Program
    {
        public async Task RunAsync()
        {
            //Configure the service provider with all relevant and required services to be injected into other classes.
            var provider = new ServiceCollection()
                .AddSingleton(x => new RavenDatabase() as IDatabase)
                .AddSingleton(x => new DiscordShardedClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = false,
                    MessageCacheSize = 0,
                    LogLevel = LogSeverity.Info,

                    //You may want to edit the shard count as the bot grows more and more popular.
                    //Discord will block single shards that try to connect to more than 2500 servers
                    TotalShards = 1
                }))
                .AddSingleton(x => new LogHandler(x.GetRequiredService<DiscordShardedClient>(), x.GetRequiredService<IDatabase>()))
                .AddSingleton(x =>
                {
                    //Initialize the bot config by asking for token and name
                    var config = x.GetRequiredService<IDatabase>().Load<BotConfig>("BotConfig");
                    if (config == null)
                    {
                        Console.WriteLine("Please enter your bot token (found at https://discordapp.com/developers/applications/ )");
                        var token = Console.ReadLine();
                        Console.WriteLine("Input a bot prefix (this will be used to run commands, ie. prefix = f. command will be f.command)");
                        var prefix = Console.ReadLine();
                        Console.WriteLine("Input a bot name (this will be used for certain database tasks)");
                        var name = Console.ReadLine();
                        config = new BotConfig(token, prefix, name);
                        x.GetRequiredService<IDatabase>().Store(config, "BotConfig");
                    }

                    return config;
                })
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    ThrowOnError = false,
                    CaseSensitiveCommands = false,
                    IgnoreExtraArgs = false,
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Info
                }))
                .AddSingleton(x =>
                    {
                        if (x.GetRequiredService<IDatabase>() is RavenDatabase rd)
                        {
                            return new GraphiteService(rd.GetGraphiteClient());
                        }

                        return null;
                    })
                .AddSingleton<TimerService>()
                .AddSingleton(x =>
                {
                    if (x.GetRequiredService<IDatabase>() is RavenDatabase rd)
                    {
                        return new PrefixService(rd, x.GetRequiredService<BotConfig>().Prefix, 
                            rd.GetOrInitializeConfig().Developer, 
                            rd.GetOrInitializeConfig().DeveloperPrefix);

                    }

                    return new PrefixService(x.GetRequiredService<IDatabase>(), x.GetRequiredService<BotConfig>().Prefix);
                })
                .AddSingleton<EventHandler>()
                .AddSingleton(x => new LicenseService(x.GetRequiredService<IDatabase>()))
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            try
            {
                await provider.GetRequiredService<EventHandler>().InitializeAsync();
                provider.GetRequiredService<TimerService>().Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            await Task.Delay(-1);
        }

        public static void Main(string[] args)
        {
            var program = new Program();
            program.RunAsync().GetAwaiter().GetResult();
        }
    }
}
