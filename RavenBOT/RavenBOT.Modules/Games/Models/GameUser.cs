namespace RavenBOT.Modules.Games.Models
{
    public class GameUser
    {
        public static string DocumentName(ulong userId, ulong guildId)
        {
            return $"GameUser-{userId}-{guildId}";
        }
        public GameUser(ulong userId, ulong guildId)
        {
            UserId = userId;
            GuildId = guildId;
            Points = 200;
        }

        public GameUser() {}

        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public int Points { get; set; }

        public int TotalBet { get; set; }

        public int TotalWon { get; set; }
    }
}