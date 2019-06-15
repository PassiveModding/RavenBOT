namespace RavenBOT.Modules.Games.Models
{
    public class GameServer
    {
        public static string DocumentName(ulong guildId)
        {
            return $"GameServer-{guildId}";
        }

        public ulong GuildId { get; set; }

        public GameServer(ulong guildId)
        {
            GuildId = guildId;
        }

        public GameServer() { }
    }
}