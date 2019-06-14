using System.Collections.Generic;
namespace RavenBOT.Modules.RoleManagement.Models
{
    public class RoleConfig
    {
        public static string DocumentName(ulong guildId) => $"RoleManagerConfig-{guildId}";

        public ulong GuildId { get; set; }

        public List<RoleManagementEmbed> RoleMessages { get; set; } = new List<RoleManagementEmbed>();

        public class RoleManagementEmbed
        {
            public List<ulong> Roles = new List<ulong>();
            public ulong MessageId { get; set; }
        }
    }
}