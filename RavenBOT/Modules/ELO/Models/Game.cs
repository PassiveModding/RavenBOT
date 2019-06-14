using System;
using System.Collections.Generic;

namespace RavenBOT.Modules.ELO.Models
{
    public class GameResult
    {
        public GameResult(int gameId)
        {
            GameId = gameId;
        }
        public int GameId { get; set; }
        public enum State
        {
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
            public List<ulong> Players { get; set; } = new List<ulong>();
        }
    }
}