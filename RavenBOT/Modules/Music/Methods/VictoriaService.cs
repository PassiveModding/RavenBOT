using System;
using System.IO;
using Discord.WebSocket;
using Newtonsoft.Json;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Music.Methods
{
    public class VictoriaService : IServiceable
    {
        public Victoria.LavaShardClient Client { get; set; } = null;
        public Victoria.LavaRestClient RestClient { get; set; } = null;
        public IDatabase Database { get; }
        private DiscordShardedClient DiscordClient { get; }
        public VictoriaService (DiscordShardedClient client, IDatabase database)
        {
            DiscordClient = client;
            Database = database;
            Configure ();
        }

        public void Configure ()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "setup", "Victoria.json");
            Victoria.Configuration config;

            if (!File.Exists(configPath))
            {
                config = new Victoria.Configuration();
                Console.WriteLine("Audio Client Setup");
                Console.WriteLine("Input Lavalink Host URL");
                config.Host = Console.ReadLine();
                
                Console.WriteLine("Input Lavalink Port");
                config.Port = int.Parse(Console.ReadLine());
                                
                Console.WriteLine("Input Lavalink Password");
                config.Password = Console.ReadLine();

                Console.WriteLine("Further audio settings can be configured in Victoria.json in the setup directory");

                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));                
            }
            else
            {
                config = JsonConvert.DeserializeObject<Victoria.Configuration>(File.ReadAllText(configPath));
            }

            Client = new Victoria.LavaShardClient();
            RestClient = new Victoria.LavaRestClient(config);
            Client.StartAsync(DiscordClient, config);
        }
    }
}