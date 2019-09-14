using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RavenBOT.Common
{
    /// <summary>
    /// Handles general guild-level configurations
    /// </summary>
    public class GuildService : IServiceable
    {
        public IDatabase Database { get; }
        public BotConfig Config { get; }
        public LocalManagementService Local { get; }

        public GuildService(IDatabase database, BotConfig config, LocalManagementService local)
        {
            this.Database = database;
            Config = config;
            this.Local = local;
        }
        public string DefaultPrefix => Local.LastConfig.Developer ? Local.LastConfig.DeveloperPrefix : Config.Prefix;

        public Dictionary<ulong, GuildConfig> Cache = new Dictionary<ulong, GuildConfig>();

        //Used to list servers that have returned null for a guildconfig to reduce database lookups
        public List<ulong> AntiCache = new List<ulong>();

        /// <summary>
        /// Try to load the config from cache, otherwise attempt to load it from the database
        /// Can return null if no config generated.
        /// </summary>
        /// <param name="guildId">the id of the guild that created the config</param>
        /// <returns>config or null</returns>
        public GuildConfig GetConfig(ulong guildId)
        {
            if (Cache.TryGetValue(guildId, out var config))
            {
                return config;
            }
            else if (AntiCache.Contains(guildId))
            {
                return null;
            }

            var res = Database.Load<GuildConfig>(GuildConfig.DocumentName(guildId));
            if (res != null)
            {
                Cache.Add(guildId, res);
            }
            else
            {
                AntiCache.Add(guildId);
            }

            return res;            
        }

        /// <summary>
        /// Saves the config and updates the cached value.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public void SaveConfig(GuildConfig config)
        {
            Cache[config.GuildId] = config;
            AntiCache.Remove(config.GuildId);
            Database.Store<GuildConfig>(config, GuildConfig.DocumentName(config.GuildId));
        }

        public string GetPrefix(ulong guildId)
        {
            var config = GetConfig(guildId);
            if (config == null)
            {
                return DefaultPrefix;
            }

            return config.PrefixOverride ?? DefaultPrefix;
        }

        public bool IsModuleAllowed(ulong guildId, string command)
        {
            //ignores blacklist if there is a non valid guild id
            if (guildId <= 0)
            {
                return true;
            }

            var config = GetConfig(guildId);
            //Return if there is no config made.
            if (config == null) return true;

            //Return if there is nothing in the blacklist
            if (config.ModuleBlacklist.Count == 0)
            {
                return true;
            }

            //Override prefix if the bot is in developer mode
            var prefix = Local.LastConfig.Developer ? Local.LastConfig.DeveloperPrefix : (config.PrefixOverride ?? DefaultPrefix);

            if (config.ModuleBlacklist.Any(x => command.StartsWith(x, true, CultureInfo.InvariantCulture) || command.StartsWith($"{prefix} {x}", true, CultureInfo.InvariantCulture) || command.StartsWith($"{prefix}{x}", true, CultureInfo.InvariantCulture)))
            {
                return false;
            }

            return true;
        }

        public class GuildConfig
        {
            public static string DocumentName(ulong guildId)
            {

                return $"GuildRavenConfig-{guildId}";
            }

            public GuildConfig(ulong guildId)
            {
                GuildId = guildId;
            }

            public GuildConfig(){}

            public ulong GuildId { get; set; }

            public bool UnknownCommandResponse { get; set; } = true;

            public HashSet<string> ModuleBlacklist { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            private string _prefixOverride = null;

            public string PrefixOverride
            {
                get
                {
                    return _prefixOverride;
                }
                //Ensure prefix override is never an empty string
                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _prefixOverride = null;
                    }
                    else
                    {
                        _prefixOverride = value;
                    }
                }
            }
        }
    }
}