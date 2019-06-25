using System.Collections.Generic;
using static RavenBOT.Modules.Levels.Modules.Invites;

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

        public Dictionary<string, TrackedInvite> TrackedInvites { get; set; } = new Dictionary<string, TrackedInvite>();
    }
}