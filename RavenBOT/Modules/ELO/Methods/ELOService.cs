using System.Linq;
using Discord.WebSocket;
using RavenBOT.Modules.ELO.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.ELO.Methods
{
    public class ELOService : IServiceable
    {
        public ELOService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }

        public CompetitionConfig CreateCompetition(ulong guildId)
        {
            var config = new CompetitionConfig(guildId);
            Database.Store(config, CompetitionConfig.DocumentName(guildId));

            return config;
        }

        public CompetitionConfig GetCompetition(ulong guildId)
        {
            return Database.Load<CompetitionConfig>(CompetitionConfig.DocumentName(guildId));
        }

        public Lobby CreateLobby(ulong guildId, ulong channelId)
        {
            var config = new Lobby(guildId, channelId);
            Database.Store(config, Lobby.DocumentName(guildId, channelId));

            return config;
        }

        public Lobby GetLobby(ulong guildId, ulong channelId)
        {
            return Database.Load<Lobby>(Lobby.DocumentName(guildId, channelId));
        }
    }
}