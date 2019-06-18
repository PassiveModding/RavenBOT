using System.Collections.Generic;

namespace RavenBOT.ELO.Modules.Models
{
    public class CompetitionConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"CompetitionConfig-{guildId}";
        }

        public CompetitionConfig(ulong guildId)
        {
            this.GuildId = guildId;
        }
        public ulong GuildId { get; set; }

        public List<Rank> Ranks { get; set; } = new List<Rank>();

        public ulong RegisteredRankId { get; set; } = 0;

        public string NameFormat { get; set; } = "[{score}] {name}";

        public bool BlockMultiQueueing { get; set; } = false;
    }
}