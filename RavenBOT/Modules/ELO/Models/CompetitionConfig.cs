using System.Collections.Generic;

namespace RavenBOT.Modules.ELO.Models
{
    public class CompetitionConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"CompetitionConfig-{guildId}";
        }

        public CompetitionConfig (ulong guildId)
        {
            this.GuildId = guildId;
        }
        public ulong GuildId { get; set; }

        //TODO: Competition settings.
        public List<Rank> Ranks {get;set;} = new List<Rank>();

        public ulong RegisteredRankId {get;set;} = 0;

        public int PlayersPerTeam {get;set;} = 5;        
    }
}