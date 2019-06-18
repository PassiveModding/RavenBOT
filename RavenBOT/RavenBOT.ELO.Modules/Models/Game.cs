using System;
using System.Collections.Generic;

namespace RavenBOT.ELO.Modules.Models
{
    public class GameResult
    {
        public static string DocumentName(int gameId, ulong lobbyId, ulong guildId)
        {
            return $"GameResult-{gameId}-{lobbyId}-{guildId}";
        }

        public GameResult(int gameId, ulong lobbyId, ulong guildId)
        {
            GameId = gameId;
            LobbyId = lobbyId;
            GuildId = guildId;
        }
        public int GameId { get; set; }

        public ulong LobbyId { get; set; }

        public ulong GuildId { get; set; }

        public enum State
        {
            Picking,
            Undecided,
            Draw,
            Decided,
            Canceled
        }

        public DateTime CreationTime { get; set; } = DateTime.UtcNow;

        public State GameState { get; set; } = State.Undecided;
        public int WinningTeam { get; set; } = -1;

        public Team Team1 { get; set; } = new Team();
        public Team Team2 { get; set; } = new Team();
        public List<ulong> Queue { get; set; } = new List<ulong>();
        public class Team
        {
            public ulong Captain { get; set; } = 0;
            public List<ulong> Players { get; set; } = new List<ulong>();
        }
    }
}