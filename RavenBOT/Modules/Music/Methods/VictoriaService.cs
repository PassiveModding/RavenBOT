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

        public class VictoriaConfig
        {
            public Victoria.Configuration MainConfig {get;set;} = new Victoria.Configuration();
            public RestConfig RestConfig {get;set;} = new RestConfig();
        }

        public class RestConfig
        {
            public string Host {get;set;}
            public int Port {get;set;}
            public string Password {get;set;}
        }

        public VictoriaService (DiscordShardedClient client, IDatabase database, LogHandler logger)
        {
            DiscordClient = client;
            Database = database;
            Logger = logger;
            if (!File.Exists(ConfigPath))
            {
                var config = new VictoriaConfig();
                Console.WriteLine("Audio Client Setup");
                Console.WriteLine("Input Lavalink Host URL");
                config.MainConfig.Host = Console.ReadLine();
                config.RestConfig.Host = config.MainConfig.Host;
                
                Console.WriteLine("Input Lavalink Port");
                config.MainConfig.Port = int.Parse(Console.ReadLine());                
                config.RestConfig.Port = config.MainConfig.Port;
                                
                Console.WriteLine("Input Lavalink Password");
                config.MainConfig.Password = Console.ReadLine();
                config.RestConfig.Password = config.MainConfig.Password;

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
            var config = JsonConvert.DeserializeObject<VictoriaConfig>(File.ReadAllText(ConfigPath));

            Client = new Victoria.LavaShardClient();
            RestClient = new Victoria.LavaRestClient(config.RestConfig.Host, config.RestConfig.Port, config.RestConfig.Password);
            await Client.StartAsync(DiscordClient, config.MainConfig);
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