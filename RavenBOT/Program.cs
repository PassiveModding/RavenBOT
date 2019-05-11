using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;
using RavenBOT.Modules.Developer.Methods;
using RavenBOT.Services;
using RavenBOT.Services.Database;
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
                .AddSingleton<LocalManagementService>()
                .AddSingleton(x =>
                {
                    //Initialize the bot config by asking for token and name
                    var config = x.GetRequiredService<IDatabase>().Load<BotConfig>("BotConfig");
                    if (config == null)
                    {
                        Console.WriteLine("Please enter your bot token (found at https://discordapp.com/developers/applications/ )");
                        var token = Console.ReadLine();
                        
                        Console.WriteLine("Input a bot name (this will be used for certain database tasks)");
                        var name = Console.ReadLine();

                        Console.WriteLine("Would you like to use a bot prefix or a module prefix? (Y for Bot Prefix, N for Module prefix)");
                        var choice = Console.ReadLine();
                        while (!choice.Equals("Y", StringComparison.InvariantCultureIgnoreCase) && !choice.Equals("N", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine("Invalid choice.");
                            choice = Console.ReadLine();
                        }

                        if (choice.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine("Input a bot prefix (this will be used to run commands, ie. prefix = f. command will be f.command)");
                            var prefix = Console.ReadLine();
                            config = new BotConfig(token, prefix, name);
                        }
                        else
                        {
                            config = new BotConfig(token, name);
                        }
                        
                        x.GetRequiredService<IDatabase>().Store(config, "BotConfig");
                    }

                    return config;
                })
                .AddSingleton<Setup>()
                .AddSingleton(x => new HelpService(x.GetRequiredService<PrefixService>(), x.GetRequiredService<CommandService>(), x.GetRequiredService<BotConfig>(), x.GetRequiredService<Setup>(), x))
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

                    var localConfig = x.GetRequiredService<LocalManagementService>().GetConfig();
                    return new PrefixService(x.GetRequiredService<IDatabase>(), x.GetRequiredService<BotConfig>().GetPrefix(), localConfig.Developer, localConfig.DeveloperPrefix);
                })
                .AddSingleton<EventHandler>()
                .AddSingleton(x => new LicenseService(x.GetRequiredService<IDatabase>()))
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            try
            {
                await provider.GetRequiredService<EventHandler>().InitializeAsync();
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
