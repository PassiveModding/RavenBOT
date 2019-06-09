using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;
using RavenBOT.Services.Licensing;
using EventHandler = RavenBOT.Handlers.EventHandler;

namespace RavenBOT
{
    public class Program
    {
        public IServiceProvider Provider { get; set; }

        public async Task RunAsync()
        {
            var localManagement = new LocalManagementService();
            var localConfig = localManagement.GetConfig();
            IDatabase database;
            switch (localConfig.DatabaseChoice)
            {
                case LocalManagementService.LocalConfig.DatabaseSelection.RavenDatabase:
                    database = new RavenDatabase();
                    break;
                case LocalManagementService.LocalConfig.DatabaseSelection.RavenEmbeddedDatabase:
                    database = new RavenDBEmbedded();
                    break;
                case LocalManagementService.LocalConfig.DatabaseSelection.LiteDatabase:
                    database = new LiteDataStore();
                    break;
                default:
                    database = new RavenDBEmbedded();
                    break;
            }

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IServiceable).IsAssignableFrom(p) && !p.IsInterface);

            IServiceCollection collection = new ServiceCollection();
            foreach (var type in types)
            {
                collection = collection.AddSingleton(type);
            }

            //Configure the service provider with all relevant and required services to be injected into other classes.
            Provider = collection
                .AddSingleton(database)
                .AddSingleton(x => new DiscordShardedClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = false,
                    MessageCacheSize = 50,
                    LogLevel = LogSeverity.Info,

                    //You may want to edit the shard count as the bot grows more and more popular.
                    //Discord will block single shards that try to connect to more than 2500 servers
                    TotalShards = 1
                }))
                .AddSingleton(x => new LogHandler(x.GetRequiredService<DiscordShardedClient>(), x.GetRequiredService<IDatabase>()))
                .AddSingleton(localManagement)
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

                        Console.WriteLine("Input a bot prefix (this will be used to run commands, ie. prefix = f. command will be f.command)");
                        var prefix = Console.ReadLine();
                        config = new BotConfig(token, prefix, name);
                        
                        x.GetRequiredService<IDatabase>().Store(config, "BotConfig");
                    }

                    return config;
                })
                .AddSingleton<DeveloperSettings>()
                .AddSingleton(x => new ModuleManagementService(x.GetRequiredService<IDatabase>(), x.GetRequiredService<PrefixService>(), localConfig.Developer))
                .AddSingleton(x => new HelpService(x.GetRequiredService<PrefixService>(), x.GetRequiredService<ModuleManagementService>(), x.GetRequiredService<CommandService>(), x.GetRequiredService<BotConfig>(), x.GetRequiredService<DeveloperSettings>(), x))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    ThrowOnError = false,
                    CaseSensitiveCommands = false,
                    IgnoreExtraArgs = false,
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Info
                }))
                .AddSingleton(x =>  new PrefixService(x.GetRequiredService<IDatabase>(), localConfig.Developer ? localConfig.DeveloperPrefix : x.GetRequiredService<BotConfig>().Prefix))
                .AddSingleton<EventHandler>()
                .AddSingleton(x => new LicenseService(x.GetRequiredService<IDatabase>()))
                .AddSingleton<InteractiveService>()
                .AddSingleton<Random>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();

            try
            {
                await Provider.GetRequiredService<EventHandler>().InitializeAsync();
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
