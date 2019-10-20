using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using RavenBOT.Common;

namespace RavenBOT.ELO.Modules.Models
{
    public class ManualGameResult
    {
        public static string DocumentName(int gameId, ulong guildId)
        {
            return $"ManualGameResult-{gameId}-{guildId}";
        }

        public ManualGameResult(){}

        public ManualGameResult(int gameId, ulong guildId)
        {
            GameId = gameId;
            GuildId = guildId;
        }

        public int GameId { get; set; }
        public ulong GuildId { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;

        public string Comment { get; set; } = null;

        public ulong Submitter { get; set; }

        public HashSet<(ulong, int)> UpdatedScores { get; set; } = new HashSet<(ulong, int)>();
    }
}