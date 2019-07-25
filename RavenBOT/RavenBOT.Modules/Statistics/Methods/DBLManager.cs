using Discord.WebSocket;
using DiscordBotsList.Api.Adapter.Discord.Net;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Modules.Statistics.Models;

namespace RavenBOT.Modules.Statistics.Methods
{
    public class DBLManager : IServiceable
    {
        public DBLManager(IDatabase database, DiscordShardedClient client, LogHandler logger)
        {
            Database = database;
            Client = client;
            Logger = logger;
            DBLApi = null;
            Initialize();
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public LogHandler Logger { get; }
        public ShardedDiscordNetDblApi DBLApi { get; set; }

        public void Initialize()
        {
            var config = Database.Load<DBLApiConfig>(DBLApiConfig.DocumentName());
            if (config == null)
            {
                //The document should be generated with the use of a command and not generated on demand.
                return;
            }

            if (config.APIKey == null)
            {
                Logger.Log("DiscordBots.org api key not set but a config was generated.", Discord.LogSeverity.Warning);
                return;
            }

            try
            {
                DBLApi = new ShardedDiscordNetDblApi(Client, config.APIKey);
                DBLApi?.CreateListener();
            }
            catch
            {
                //
            }
        }

        public DBLApiConfig GetOrCreateConfig()
        {
            var config = Database.Load<DBLApiConfig>(DBLApiConfig.DocumentName());
            if (config == null)
            {
                config = new DBLApiConfig();
                Database.Store(config, DBLApiConfig.DocumentName());
            }

            return config;
        }

        public void SaveConfig(DBLApiConfig config)
        {
            Database.Store(config, DBLApiConfig.DocumentName());
        }
    }
}