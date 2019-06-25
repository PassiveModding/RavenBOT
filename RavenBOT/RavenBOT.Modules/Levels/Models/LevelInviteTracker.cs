using System.Collections.Generic;

namespace RavenBOT.Modules.Levels.Models
{
    public class LevelInviteTracker
    {
        public static string DocumentName(ulong guildId)
        {
            return $"InviteTracker-{guildId}";
        }

        public ulong GuildId { get; set; }

        public bool Enabled { get; set; } = false;

        public Dictionary<string, Modules.Level.TrackedInvite> TrackedInvites { get; set; } = new Dictionary<string, Modules.Level.TrackedInvite>();
    }
}