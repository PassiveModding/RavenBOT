using System.Collections.Generic;
namespace RavenBOT.Modules.Media.Models
{
    public class AttachmentRandomConfig
    {
        public static string DocumentName(ulong guildId, string key)
        {
            return $"AttachmentRandom-{guildId}-{key}";
        }
        public string Key { get; set; }

        public AttachmentRandomConfig(ulong guildId, string key)
        {
            this.Key = key;
            GuildId = guildId;
        }

        public ulong GuildId { get; set; }
        public List<string> AttachmentUrls { get; set; } = new List<string>();
    }
}