namespace RavenBOT.Models
{
    public class BotConfig
    {
        public BotConfig()
        {

        }

        public BotConfig(string token, string name)
        {
            Token = token;
            Name = name;
            UsePrefixSystem = false;
        }

        public BotConfig(string token, string prefix, string name)
        {
            Token = token;
            UsePrefixSystem = true;
            Prefix = prefix;
            Name = name;
        }

        public string Token { get; set; }
        private string Prefix { get; set; }

        public string GetPrefix()
        {
            if (UsePrefixSystem)
            {
                return Prefix;
            }

            return null;
        }

        public string Name { get; set; }
        public bool UsePrefixSystem { get; set; }
    }
}
