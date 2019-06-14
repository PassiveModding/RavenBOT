namespace RavenBOT.Modules.Levels.Models
{
    public class LevelUser
    {
        public LevelUser(ulong userId, ulong guildId)
        {
            UserId = userId;
            GuildId = guildId;
        }

        public LevelUser() { }

        public static string DocumentName(ulong userId, ulong guildId)
        {
            return $"LevelUser-{guildId}-{userId}";
        }

        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public int UserXP { get; set; } = 0;

        public int UserLevel { get; set; } = 1;
    }
}