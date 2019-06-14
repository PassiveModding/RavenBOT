using System.Collections.Generic;

namespace RavenBOT.Modules.Moderator.Models
{
    public class ModeratorConfig
    {
        public static string DocumentName(ulong guildId) => $"ModeratorConfig-{guildId}";
        public ulong GuildId { get; set; }
        public List<ulong> ModeratorRoles { get; set; } = new List<ulong>();
    }
}