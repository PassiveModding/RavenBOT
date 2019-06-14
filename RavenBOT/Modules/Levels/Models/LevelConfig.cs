using System.Collections.Generic;

namespace RavenBOT.Modules.Levels.Models
{
    public class LevelConfig
    {
        public LevelConfig(ulong guildId)
        {
            GuildId = guildId;
        }

        public LevelConfig() { }

        public static string DocumentName(ulong guildId)
        {
            return $"LevelConfig-{guildId}";
        }

        public ulong GuildId { get; set; }
        public bool Enabled { get; set; } = false;

        //User keeps 1 level role only if this is false
        //Otherwise they keep a role for each, reward
        public bool MultiRole { get; set; } = false;

        public ulong LogChannelId { get; set; } = 0;
        public bool ReplyLevelUps { get; set; } = true;

        public List<LevelReward> RewardRoles { get; set; } = new List<LevelReward>();

        public class LevelReward
        {
            public ulong RoleId { get; set; }
            public int LevelRequirement { get; set; }
        }
    }
}