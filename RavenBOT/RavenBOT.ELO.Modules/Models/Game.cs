using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using RavenBOT.Common;

namespace RavenBOT.ELO.Modules.Models
{
    public class GameResult
    {
        public static string DocumentName(int gameId, ulong lobbyId, ulong guildId)
        {
            return $"GameResult-{gameId}-{lobbyId}-{guildId}";
        }

        public GameResult(){}

        public GameResult(int gameId, ulong lobbyId, ulong guildId, Lobby.PickMode lobbyPickMode)
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

        public string Comment { get; set; } = null;

        public enum CaptainPickOrder
        {
            PickOne,
            PickTwo
        }

        public ulong Submitter { get; set; }

        public CaptainPickOrder PickOrder { get; set; } = CaptainPickOrder.PickTwo;

        public Lobby.PickMode GamePickMode { get; set; }

        public int WinningTeam { get; set; } = -1;

        public Team Team1 { get; set; } = new Team();
        public Team Team2 { get; set; } = new Team();
        public HashSet<ulong> Queue { get; set; } = new HashSet<ulong>();

        public int Picks { get; set; } = 0;

        public class Team
        {
            public ulong Captain { get; set; } = 0;
            public HashSet<ulong> Players { get; set; } = new HashSet<ulong>();

            public async Task<string> GetTeamInfo(SocketGuild guild)
            {
                var resStr = "";
                //Only show captain info if a captain has been set.
                if (Captain != 0)
                {
                    resStr += $"Captain: {guild.GetUser(Captain)?.Mention ?? $"[{Captain}]"}\n";
                }

                if (Players.Any())
                {
                    resStr += $"Players: {string.Join("\n", await guild.GetUserMentionListAsync(Players.Where(x => x != Captain)))}";
                }
                else
                {
                    resStr += "Players: N/A";
                }

                return resStr;
            }
        }

        //Indicates user IDs and the amount of points added/removed from them when the game result was decided.
        public HashSet<(ulong, int)> UpdatedScores { get; set; } = new HashSet<(ulong, int)>();

        /// <summary>
        /// Returns the channel that this game was created in
        /// Will return null if the channel is unavailable/deleted
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public SocketTextChannel GetChannel(SocketGuild guild)
        {
            if (guild.Id != GuildId)
            {
                throw new ArgumentException("Guild provided must be the same as the guild this game was created in.");
            }
            return guild.GetTextChannel(LobbyId);
        }

        public IEnumerable<ulong> GetQueueRemainingPlayers()
        {
            return Queue.Where(x => Team1.Captain != x && !Team1.Players.Contains(x) && Team2.Captain != x && !Team2.Players.Contains(x));
        }

        public async Task<string> GetQueueRemainingPlayersString(SocketGuild guild)
        {
            return string.Join("\n", await guild.GetUserMentionListAsync(GetQueueRemainingPlayers()));
        }

        public async Task<string> GetQueueMentionList(SocketGuild guild)
        {
            return string.Join("\n", await guild.GetUserMentionListAsync(Queue));
        }

        public (int, Team) GetWinningTeam()
        {
            if (WinningTeam == 1)
            {
                return (1, Team1);
            }
            else if (WinningTeam == 2)
            {
                return (2, Team2);
            }

            return (-1, null);
        }

        public (int, Team) GetLosingTeam()
        {
            if (WinningTeam == 1)
            {
                return (2, Team2);
            }
            else if (WinningTeam == 2)
            {
                return (1, Team1);
            }
            
            return (-1, null);
        }
    }
}