using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Modules.Games.Models;

namespace RavenBOT.Modules.Games.Methods
{
    public class GameService : IServiceable
    {
        private IDatabase Database { get; }

        public GameService(IDatabase database)
        {
            Database = database;
        }

        public GameServer GetGameServer(ulong guildId)
        {
            var server = Database.Load<GameServer>(GameServer.DocumentName(guildId));

            if (server == null)
            {
                server = new GameServer(guildId);
                Database.Store(server, GameServer.DocumentName(guildId));
            }

            return server;
        }

        public GameUser GetGameUser(ulong userId, ulong guildId)
        {
            var user = Database.Load<GameUser>(GameUser.DocumentName(userId, guildId));

            if (user == null)
            {
                user = new GameUser(userId, guildId);
                Database.Store(user, GameUser.DocumentName(userId, guildId));
            }

            return user;
        }

        public void SaveGameServer(GameServer server)
        {
            Database.Store(server, GameServer.DocumentName(server.GuildId));
        }

        public void SaveGameUser(GameUser user)
        {
            Database.Store(user, GameUser.DocumentName(user.UserId, user.GuildId));
        }
    }
}