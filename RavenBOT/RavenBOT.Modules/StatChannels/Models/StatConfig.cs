namespace RavenBOT.Modules.StatChannels.Models
{
    public class StatConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"StatConfig-{guildId}";
        }

        public StatConfig(ulong guildId)
        {
            GuildId = guildId;
        }

        public StatConfig() { }

        public ulong GuildId { get; set; }
        public ulong UserCountChannelId { get; set; } = 0;
    }
}