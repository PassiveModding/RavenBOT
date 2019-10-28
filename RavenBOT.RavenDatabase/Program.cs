using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Common;
using EventHandler = RavenBOT.Handlers.EventHandler;
using CommandLine;
using RavenBOT.Common.Interfaces.Database;

namespace RavenBOT
{
    public class Program
    {
        public class Options
        {
            [Option('p', "path", Required = false, HelpText = "Path to a LocalConfig.json file")]
            public string Path { get; set; }
        }

        public static IServiceProvider Provider { get; set; }
        public async Task RunAsync(string[] args = null)
        {
            if (args != null)
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(o =>
                    {
                        if (o.Path != null)
                        {
                            LocalManagementService.ConfigPath = o.Path;
                        }
                    });
            }            

            var localManagement = new LocalManagementService();
            IDatabase database = new RavenDatabase(localManagement);

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
                        ExclusiveBulkDelete = true,

                        //You may want to edit the shard count as the bot grows more and more popular.
                        //Discord will block single shards that try to connect to more than 2500 servers
                        //May be advisable to fetch from a config file OR default to 1
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
                .AddSingleton<GuildService>()
                .AddSingleton(x => new HelpService(x.GetRequiredService<CommandService>(), x.GetRequiredService<BotConfig>(), x.GetRequiredService<GuildService>(), x.GetRequiredService<DeveloperSettings>(), x))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    ThrowOnError = false,
                        CaseSensitiveCommands = false,
                        IgnoreExtraArgs = false,
                        DefaultRunMode = RunMode.Async,
                        LogLevel = LogSeverity.Info
                }))
                .AddSingleton<EventHandler>()
                .AddSingleton(x => new LicenseService(x.GetRequiredService<IDatabase>()))
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
            program.RunAsync(args).GetAwaiter().GetResult();
        }
    }
}