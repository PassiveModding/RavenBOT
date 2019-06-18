namespace RavenBOT.Modules.Greetings.Models
{
    public class GoodbyeConfig
    {
        public GoodbyeConfig(ulong guildId)
        {
            GuildId = guildId;
        }
        public GoodbyeConfig() {}

        public ulong GoodbyeChannel { get; set; }
        public ulong GuildId { get; set; }
        public bool Enabled { get; set; } = false;
        public bool DirectMessage { get; set; } = false;
        public string GoodbyeMessage { get; set; }

        public static string DocumentName(ulong guildId)
        {
            return $"GoodbyeConfig-{guildId}";
        }
    }
}