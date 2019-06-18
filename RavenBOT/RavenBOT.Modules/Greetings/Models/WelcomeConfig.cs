namespace RavenBOT.Modules.Greetings.Models
{
    public class WelcomeConfig
    {
        public WelcomeConfig(ulong guildId)
        {
            GuildId = guildId;
        }
        public WelcomeConfig() {}

        public ulong WelcomeChannel { get; set; }
        public ulong GuildId { get; set; }
        public bool Enabled { get; set; } = false;
        public bool DirectMessage { get; set; } = false;
        public string WelcomeMessage { get; set; }

        public static string DocumentName(ulong guildId)
        {
            return $"WelcomeConfig-{guildId}";
        }
    }
}