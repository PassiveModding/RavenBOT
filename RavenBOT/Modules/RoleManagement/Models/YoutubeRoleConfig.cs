using System.Collections.Generic;
namespace RavenBOT.Modules.RoleManagement.Models
{
    public class YoutubeRoleConfig
    {
        public static string DocumentName(ulong guildId) => $"YoutubeRoleConfig-{guildId}";

        public ulong GuildId { get; set; }

        public Dictionary<string, SubReward> SubRewards { get; set; } = new Dictionary<string, SubReward>();

        public class SubReward
        {
            public ulong RewardedRoleId  { get; set; }
            public string YoutubeChannelId { get; set; }
            public string DisplayName { get; set; }
            public List<YoutubeSubscriber> AuthenticatedUserIds { get; set; } = new List<YoutubeSubscriber>();

            public class YoutubeSubscriber
            {
                public string YoutubeChannelId { get; set; }
                public ulong UserId { get; set; }
            }
        }
    }
}