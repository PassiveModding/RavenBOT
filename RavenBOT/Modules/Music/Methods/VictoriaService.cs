using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using RavenBOT.Handlers;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Music.Methods
{
    public class VictoriaService : IServiceable
    {
        public Victoria.LavaShardClient Client { get; set; } = null;
        public Victoria.LavaRestClient RestClient { get; set; } = null;
        public IDatabase Database { get; }
        public LogHandler Logger { get; }
        private DiscordShardedClient DiscordClient { get; }

        private readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "setup", "Victoria.json");

        public VictoriaService (DiscordShardedClient client, IDatabase database, LogHandler logger)
        {
            DiscordClient = client;
            Database = database;
            Logger = logger;
            if (!File.Exists(ConfigPath))
            {
                var config = new Victoria.Configuration();
                Console.WriteLine("Audio Client Setup");
                Console.WriteLine("Input Lavalink Host URL");
                config.Host = Console.ReadLine();
                
                Console.WriteLine("Input Lavalink Port");
                config.Port = int.Parse(Console.ReadLine());
                                
                Console.WriteLine("Input Lavalink Password");
                config.Password = Console.ReadLine();

                Console.WriteLine("Further audio settings can be configured in Victoria.json in the setup directory");

                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));                
            }

            DiscordClient.ShardConnected += Configure;
        }

        public async Task Configure(DiscordSocketClient sClient)
        {
            if (!DiscordClient.Shards.All(x => x.ConnectionState == Discord.ConnectionState.Connected))
            {
                return;
            }

            Logger.Log("Victoria Initializing...");
            var config = JsonConvert.DeserializeObject<Victoria.Configuration>(File.ReadAllText(ConfigPath));

            Client = new Victoria.LavaShardClient();
            RestClient = new Victoria.LavaRestClient(config);
            await Client.StartAsync(DiscordClient, config);
            Logger.Log("Victoria Initialized.");

            Client.Log += Log;
        }

        public async Task Log(LogMessage message)
        {
            Logger.Log(message.Message + message.Exception?.ToString(), message.Severity);
        }

        public bool IsConfigured ()
        {
            return Client != null && RestClient != null;
        }
    }
}