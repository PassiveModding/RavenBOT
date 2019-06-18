namespace RavenBOT.Modules.Birthday.Models
{
    public class BirthdayConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"BirthdayConfig-{guildId}";
        }

        public BirthdayConfig(ulong guildId)
        {
            GuildId = guildId;
        }
        public BirthdayConfig() {}

        public ulong BirthdayRole { get; set; }

        public ulong GuildId { get; set; }
        public ulong BirthdayAnnouncementChannelId { get; set; }
        public bool Enabled { get; set; } = false;
    }
}