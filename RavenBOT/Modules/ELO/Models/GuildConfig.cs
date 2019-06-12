namespace RavenBOT.Modules.ELO.Models
{
    public class GuildConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"GuildConfig-{guildId}";
        }
        
        public GuildConfig (ulong guildId)
        {
            this.GuildId = guildId;

        }
        public ulong GuildId { get; set; }  

        public string NameFormat {get;set;} = "[{score}] {name}";      
    }
}