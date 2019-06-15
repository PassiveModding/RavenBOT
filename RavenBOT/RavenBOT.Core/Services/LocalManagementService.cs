using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RavenBOT.Services
{
    public class LocalManagementService
    {
        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "setup");
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "Local.json");

        public LocalConfig LastConfig { get; set; }

        public LocalConfig GetConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return null;
            }

            var config = JsonConvert.DeserializeObject<LocalConfig>(File.ReadAllText(ConfigPath));
            LastConfig = config;
            return config;
        }

        public void SaveConfig(LocalConfig config)
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        public LocalManagementService()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            var config = GetConfig();
            if (config == null)
            {
                config = new LocalConfig();

                Console.WriteLine("Please indicate Y for developer mode or N for regular mode. (This will override the default prefix with a different one)");
                var devMode = Console.ReadLine();
                if (!devMode.Equals("Y", StringComparison.InvariantCultureIgnoreCase) && !devMode.Equals("N", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("Invalid Input.");
                }

                config.Developer = devMode.Equals("Y", StringComparison.InvariantCultureIgnoreCase);

                Console.WriteLine("Please enter a developer prefix for the bot.");

                config.DeveloperPrefix = Console.ReadLine();

                Console.WriteLine("Please select a database solution to use.");
                foreach (var name in Enum.GetNames(typeof(LocalConfig.DatabaseSelection)))
                {
                    Console.WriteLine(name);
                }

                LocalConfig.DatabaseSelection selection;
                do
                {
                    Console.WriteLine("Database Selection: ");
                }
                while (!Enum.TryParse(Console.ReadLine(), true, out selection));
                config.DatabaseChoice = selection;

                SaveConfig(config);

                Console.WriteLine($"Developer config saved to: {ConfigPath}");
            }
            LastConfig = config;
        }

        public class LocalConfig
        {
            public bool Developer { get; set; } = false;

            public string DeveloperPrefix { get; set; } = "dev.";

            //Used for whitelisting commands/events while in dev mode.
            public List<ulong> WhitelistedGuilds { get; set; } = new List<ulong>();

            public bool EnforceWhitelist { get; set; } = true;
            public bool IsAcceptable(ulong guildId)
            {
                if (!EnforceWhitelist)
                {
                    return true;
                }

                if (Developer)
                {
                    if (WhitelistedGuilds.Any())
                    {
                        return WhitelistedGuilds.Contains(guildId);
                    }

                    return true;
                }

                return true;
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public DatabaseSelection DatabaseChoice { get; set; } = DatabaseSelection.RavenEmbeddedDatabase;

            public enum DatabaseSelection
            {
                RavenDatabase,
                RavenEmbeddedDatabase,
                LiteDatabase
            }
        }
    }
}