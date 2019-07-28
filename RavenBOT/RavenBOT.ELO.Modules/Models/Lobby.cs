using System.Collections.Generic;

namespace RavenBOT.ELO.Modules.Models
{
    public class Lobby
    {
        public static string DocumentName(ulong guildId, ulong channelId)
        {
            return $"LobbyConfig-{guildId}-{channelId}";
        }

        public Lobby(ulong guildId, ulong channelId)
        {
            this.GuildId = guildId;
            this.ChannelId = channelId;
        }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }

        public int? MinimumPoints { get; set; } = null;

        public int PlayersPerTeam { get; set; } = 5;

        public int TeamCount { get; set; } = 2;

        public List<ulong> Queue { get; set; } = new List<ulong>();

        public PickMode TeamPickMode { get; set; } = PickMode.Random;

        public int CurrentGameCount { get; set; } = 0;

        //TODO: Specific announcement channel per lobby

        public enum PickMode
        {
            Captains_HighestRanked,
            Captains_RandomHighestRanked,
            Captains_Random,
            Random,
            TryBalance
        }

        //TODO: Allow for votes on maps, reduce change of repeate games on the same map.
        public List<string> Maps { get; set; } = new List<string>();
        public List<string> MapHistory { get; set; } = new List<string>();
    }
}