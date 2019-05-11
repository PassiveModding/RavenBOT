using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace RavenBOT.Services
{
    public class LocalManagementService
    {
        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "setup");
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "Local.json");

        public LocalConfig GetConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return null;
            }

            var config = JsonConvert.DeserializeObject<LocalConfig>(File.ReadAllText(ConfigPath));
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

                string devMode;
                do
                {
                    devMode = Console.ReadLine();
                } while (!devMode.Equals("Y", StringComparison.InvariantCultureIgnoreCase) && !devMode.Equals("N", StringComparison.InvariantCultureIgnoreCase));

                config.Developer = devMode.Equals("Y", StringComparison.InvariantCultureIgnoreCase);

                Console.WriteLine("Please enter a developer prefix for the bot.");

                config.DeveloperPrefix = Console.ReadLine();

                SaveConfig(config);

                Console.WriteLine($"Developer config saved to: {ConfigPath}");
            }
        }

        public class LocalConfig
        {
            public bool Developer { get; set; } = false;

            public string DeveloperPrefix { get; set; } = "dev.";
        }
    }
}
