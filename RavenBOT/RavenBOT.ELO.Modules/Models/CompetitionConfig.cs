using System;
using System.Collections.Generic;

namespace RavenBOT.ELO.Modules.Models
{
    public class CompetitionConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"CompetitionConfig-{guildId}";
        }

        public CompetitionConfig(){}
        public CompetitionConfig(ulong guildId)
        {
            this.GuildId = guildId;
            this.DefaultLossModifier = 5;
        }

        public ulong GuildId { get; set; }

        public List<Rank> Ranks { get; set; } = new List<Rank>();

        //TODO: Automatically generate registration role instead of requiring one to be set?
        public ulong RegisteredRankId { get; set; } = 0;

        public string NameFormat { get; set; } = "[{score}] {name}";

        public bool BlockMultiQueueing { get; set; } = false;

        public bool AllowNegativeScore { get; set; } = false;

        //TODO: Consider adding a setter to ensure value is always positive.
        public int DefaultWinModifier { get; set; } = 10;

        private int _DefaultLossModifier;
        
        public int DefaultLossModifier { 
        get
        {
            return _DefaultLossModifier;
        } set
        {
            //Ensure the value that gets set is positive as it will be subtracted from scores.
            _DefaultLossModifier = Math.Abs(value);
            
        } }
    }
}