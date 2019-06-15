namespace RavenBOT.Models
{
    public class BotConfig
    {
        public BotConfig()
        {

        }

        public BotConfig(string token, string prefix, string name)
        {
            Token = token;
            Prefix = prefix;
            Name = name;
        }

        public string Token { get; set; }
        public string Prefix { get; set; }

        public string Name { get; set; }
    }
}