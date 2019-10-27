using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace RavenBOT.Common
{
    public class LocalManagementService
    {
        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "setup");
        public static string ConfigPath = Path.Combine(ConfigDirectory, "Local.json");

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

                if (config.Developer)
                {
                    Console.WriteLine("You selected developer mode for the bot, would you like to ensure most functions can only run in whitelisted servers while developer mode is set to true? Y/N");
                    var whitelist = Console.ReadLine();
                    config.EnforceWhitelist = whitelist.Equals("Y", StringComparison.InvariantCultureIgnoreCase);

                    if (config.EnforceWhitelist)
                    {
                        Console.WriteLine("Whitelisting is now enforced, please input a guild (server) id that functions will be limited to (you can add more by manually editing the config.json)");
                        var guildId = Console.ReadLine();
                        if (ulong.TryParse(guildId, out var res))
                        {
                            config.WhitelistedGuilds.Add(res);
                        }
                        else
                        {
                            Console.WriteLine("Whitelisted guild must be a ulong datatype ie. 1234567890 Skipping whitelisting guild, you can edit this manually in the config file.");
                        }
                    }
                }

                SaveConfig(config);

                Console.WriteLine($"Developer config saved to: {ConfigPath}");
            }
            LastConfig = config;
        }

        public class LocalConfig
        {
            public bool Developer { get; set; } = false;

            public bool IgnoreBotInput { get; set; } = true;

            public string DeveloperPrefix { get; set; } = "dev.";

            //Used for whitelisting commands/events while in dev mode.
            public HashSet<ulong> WhitelistedGuilds { get; set; } = new HashSet<ulong>();

            public bool EnforceWhitelist { get; set; } = false;
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

            public Dictionary<string, object> AdditionalConfigs { get; set; } = new Dictionary<string, object>();
            public T GetConfig<T>(string key)
            {
                if (AdditionalConfigs.ContainsKey(key))
                {
                    var match = AdditionalConfigs[key] as JObject;
                    return match.ToObject<T>();
                }
                else
                {
                    return default;
                }
            }
        }
    }
}