using System.Linq;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Methods
{
    public partial class ELOService : IServiceable
    {
        public ELOService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
        }

        //TODO: Hook channel deleted event to automatically delete lobbies

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

        public GameResult GetGame(ulong guildId, ulong channelId, int gameId)
        {
            return Database.Load<GameResult>(GameResult.DocumentName(gameId, channelId, guildId));
        }

        public Lobby[] GetLobbies(ulong guildId)
        {
            return Database.Query<Lobby>(x => x.GuildId == guildId).ToArray();
        }

        public Player GetPlayer(ulong guildId, ulong userId)
        {
            return Database.Load<Player>(Player.DocumentName(guildId, userId));
        }

        public Player CreatePlayer(ulong guildId, ulong userId, string name)
        {
            var config = new Player(guildId, userId, name);
            Database.Store(config, Player.DocumentName(guildId, userId));

            return config;
        }
    }
}