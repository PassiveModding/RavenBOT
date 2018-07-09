namespace RavenBOT
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.Commands;

    using global::Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    using RavenBOT.Handlers;
    using RavenBOT.Models;

    using EventHandler = Handlers.EventHandler;
    using InteractiveService = Discord.Context.Interactive;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Gets or sets The client.
        /// </summary>
        public static DiscordShardedClient Client { get; set; }

        /// <summary>
        /// Entry point of the program
        /// </summary>
        /// <param name="args">Discarded Args</param>
        public static void Main(string[] args)
        {
            try
            {
                StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
            
        }

        /// <summary>
        /// Initialization of our service provider and bot
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static async Task StartAsync()
        {
            // This ensures that our bots setup directory is initialized and will be were the database config is stored.
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }

            var services = new ServiceCollection()
                    .AddSingleton<DatabaseHandler>()
                    .AddSingleton(x => x.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, id: "Config"))
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        ThrowOnError = false,
                        IgnoreExtraArgs = false,
                        DefaultRunMode = RunMode.Sync
                    }))
                    .AddSingleton<BotHandler>()
                    .AddSingleton<EventHandler>()
                    .AddSingleton<InteractiveService>()
                    .AddSingleton(new Random(Guid.NewGuid().GetHashCode()));

            var provider = services.BuildServiceProvider();

            // This method ensures that the database is
            // 1. Running
            // 2. Set up properly 
            // 3. contains the bot config itself
            var dbConfig = provider.GetRequiredService<DatabaseHandler>().Initialize();

            // The provider is split here so we can get our shard count from the database before actually logging into discord.
            // This is important to do so the bot always logs in with the required amount of shards.
            var shards = provider.GetRequiredService<DatabaseHandler>().Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, id: "Config").Shards;
            services.AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 20,
                AlwaysDownloadUsers = true,
                LogLevel = dbConfig.Local.Developing ? LogSeverity.Debug : LogSeverity.Warning,

                // Please change increase this as your server count grows beyond 2000 guilds. ie. < 2000 = 1, 2000 = 2, 4000 = 2 ...
                TotalShards = shards
            }));

            // Build the service provider a second time so that the ShardedClient is now included.
            provider = services.BuildServiceProvider();

            await provider.GetRequiredService<BotHandler>().InitializeAsync();
            await provider.GetRequiredService<EventHandler>().InitializeAsync(dbConfig);

            // Indefinitely delay the method from finishing so that the program stays running until stopped.
            await Task.Delay(-1);
        }
    }
}