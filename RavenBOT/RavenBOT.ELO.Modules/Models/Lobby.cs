using System;
using System.Collections.Generic;
using RavenBOT.ELO.Modules.Modules;

namespace RavenBOT.ELO.Modules.Models
{
    public class Lobby : EloLobby
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

        public Lobby(){}

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }

        public int? MinimumPoints { get; set; } = null;

        public int PlayersPerTeam { get; set; } = 5;

        public HashSet<ulong> Queue { get; set; } = new HashSet<ulong>();

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
        public HashSet<string> Maps { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        //TODO: Instead of storing map history here use the maps that were chosen for recent games?
        public List<string> MapHistory { get; set; } = new List<string>();
    }
}