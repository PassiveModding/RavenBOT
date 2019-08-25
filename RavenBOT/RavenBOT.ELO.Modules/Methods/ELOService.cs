using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.ELO.Modules.Models;
using System;
using RavenBOT.Common.Interfaces.Database;

namespace RavenBOT.ELO.Modules.Methods
{
    public partial class ELOService : IServiceable
    {
        //TODO: Event hook for player joins/updates?
        public ELOService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
        }

        //TODO: Hook channel deleted event to automatically delete lobbies
        //TODO: Caching for users and other related objects.

        private IDatabase Database { get; }
        public DiscordShardedClient Client { get; }

        public CompetitionConfig CreateCompetition(ulong guildId)
        {
            var config = new CompetitionConfig(guildId);
            SaveCompetition(config);

            return config;
        }

        public CompetitionConfig GetOrCreateCompetition(ulong guildId)
        {
            return Database.Load<CompetitionConfig>(CompetitionConfig.DocumentName(guildId)) ?? CreateCompetition(guildId);
        }

        public void SaveCompetition(CompetitionConfig comp)
        {
            Database.Store<CompetitionConfig>(comp, CompetitionConfig.DocumentName(comp.GuildId));
        }

        public (bool, Lobby) IsLobby(ulong guildId, ulong channelId)
        {
            var lobby = GetLobby(guildId, channelId);

            if (lobby == null)
            {
                return (false, null);
            }

            return (true, lobby);
        }

        public GameResult GetCurrentGame(Lobby lobby)
        {
            return GetCurrentGame(lobby.GuildId, lobby.ChannelId);
        }

        public GameResult GetCurrentGame(ulong guildId, ulong lobbyId)
        {
            var gameMatches = Database.Query<GameResult>(x => x.GuildId == guildId && x.LobbyId == lobbyId).ToArray();
            if (gameMatches.Any())
            {
                int mostRecent = gameMatches.Max(x => x.GameId);
                return gameMatches.FirstOrDefault(x => x.GameId == mostRecent);
            }

            return null;
        }

        public GameResult GetGame(ulong guildId, ulong channelId, int gameId)
        {
            return Database.Load<GameResult>(GameResult.DocumentName(gameId, channelId, guildId));
        }

        public void RemoveGame(GameResult game)
        {
            Database.Remove<GameResult>(GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
        }

        public void SaveGame(GameResult game)
        {
            Database.Store<GameResult>(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
        }

        public GameResult[] GetGames(ulong guildId, ulong channelId)
        {
            if (Database is LiteDataStore lsd)
            {
                return lsd.QuerySome<GameResult>(x => x.GuildId == guildId && x.LobbyId == channelId, 100).ToArray();
            }
            return Database.Query<GameResult>(x => x.GuildId == guildId && x.LobbyId == channelId).ToArray();
        }

        public Lobby[] GetLobbies(ulong guildId)
        {
            return Database.Query<Lobby>(x => x.GuildId == guildId).ToArray();
        }

        public Lobby CreateLobby(ulong guildId, ulong channelId)
        {
            var config = new Lobby(guildId, channelId);
            SaveLobby(config);

            return config;
        }

        public Lobby GetLobby(ulong guildId, ulong channelId)
        {
            return Database.Load<Lobby>(Lobby.DocumentName(guildId, channelId));
        }

        public void SaveLobby(Lobby lobby)
        {
            Database.Store<Lobby>(lobby, Lobby.DocumentName(lobby.GuildId, lobby.ChannelId));
        }

        /// <summary>
        /// Tries to get a player from the database, will return null if they are not found.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Player GetPlayer(ulong guildId, ulong userId)
        {
            return Database.Load<Player>(Player.DocumentName(guildId, userId));
        }

        public void SavePlayer(Player player)
        {
            Database.Store<Player>(player, Player.DocumentName(player.GuildId, player.UserId));
        }

        public void RemovePlayer(Player player)
        {
            Database.Remove<Player>(Player.DocumentName(player.GuildId, player.UserId));
        }

        
        public void SavePlayers(IEnumerable<Player> players)
        {
            Database.StoreMany<Player>(players.ToList(), x => Player.DocumentName(x.GuildId, x.UserId));
        }

        public IEnumerable<Player> GetPlayers(Expression<Func<Player, bool>> queryFunc)
        {
            return Database.Query<Player>(queryFunc);
        }

        public Player CreatePlayer(ulong guildId, ulong userId, string name)
        {
            var config = new Player(userId, guildId, name);
            SavePlayer(config);

            return config;
        }
    }
}