using System;
using System.IO;
using Newtonsoft.Json;
using RavenBOT.Handlers;
using Serilog;

namespace RavenBOT.Models
{
    public class ConfigModel
    {
        [JsonIgnore] public static readonly string Appdir = AppContext.BaseDirectory;


        public static string ConfigPath = Path.Combine(AppContext.BaseDirectory, "setup/config.json");

        public string Prefix { get; set; } = "=";
        public string Token { get; set; } = "Token";
        public bool AutoRun { get; set; }
        public string DiscordInvite { get; set; } = "https://discord.me/passive";
        public string DBName { get; set; } = "RavenBOT";
        public string DBUrl { get; set; } = "http://127.0.0.1:8080";

        public void Save(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            File.WriteAllText(file, ToJson());
        }

        public static ConfigModel Load(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            return JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(file));
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static void CheckExistence()
        {
            bool auto;
            try
            {
                auto = Load().AutoRun;
            }
            catch
            {
                auto = false;
            }

            if (!auto)
            {
                LogHandler.LogMessage("Run (Y for run, N for setup Config)");

                LogHandler.LogMessage("Y or N: ");
                var res = Console.ReadLine();
                if (res == "N" || res == "n")
                    File.Delete("setup/config.json");

                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }


            if (!File.Exists(ConfigPath))
            {
                var cfg = new ConfigModel();

                LogHandler.LogMessage(@"Please enter a prefix for the bot eg. '+' (do not include the '' outside of the prefix)");
                Console.Write("Prefix: ");
                cfg.Prefix = Console.ReadLine();
                LogHandler.LogMessage(@"Please enter a Database Name for the bot eg. 'RavenBOT' (do not include the '' outside of the name)");
                Console.Write("Database Name: ");
                cfg.DBName = Console.ReadLine();
                LogHandler.LogMessage(@"After you input your token, a config will be generated at 'setup/config.json'");
                Console.Write("Token: ");
                cfg.Token = Console.ReadLine();

                LogHandler.LogMessage("Would you like to AutoRun the bot from now on? Y/N");
                var type2 = Console.ReadKey();
                cfg.AutoRun = type2.KeyChar.ToString().ToLower() == "y";

                cfg.Save();
            }

            LogHandler.LogMessage("Config Loaded!");
            LogHandler.LogMessage($"Prefix: {Load().Prefix}");
            LogHandler.LogMessage($"Token Length: {Load().Token.Length} (should be 59)");
            LogHandler.LogMessage($"Autorun: {Load().AutoRun}");
        }
    }
}