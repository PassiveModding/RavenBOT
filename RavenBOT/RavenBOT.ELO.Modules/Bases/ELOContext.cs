using System;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Bases
{
    public class ELOContext : ShardedCommandContext
    {
        public ELOService Service { get; }

        /// <summary>
        /// The current player, this is set by default using the IsRegistered preconditon
        /// </summary>
        /// <value></value>
        public Player CurrentPlayer { get; set; }

        /// <summary>
        /// The current lobby, this is set by default using the IsLobby precondition
        /// </summary>
        /// <value></value>
        public Lobby CurrentLobby { get; set; }

        /// <summary>
        /// Gets the current game or returns null.
        /// </summary>
        /// <returns></returns>
        public GameResult GetCurrentGame()
        {
            var gameMatches = Service.Database.Query<GameResult>(x => x.GuildId == Guild.Id && x.LobbyId == Channel.Id).ToArray();
            if (gameMatches.Any())
            {
                int mostRecent = gameMatches.Max(x => x.GameId);
                return gameMatches.FirstOrDefault(x => x.GameId == mostRecent);
            }

            return null;
        }

        public ELOContext(DiscordShardedClient client, SocketUserMessage msg, IServiceProvider provider) : base(client, msg)
        {
            Service = provider.GetRequiredService<ELOService>();
        }
    }
}