using System.Collections.Generic;

namespace RavenBOT.Common
{
    /// <summary>
    /// Handles general guild-level configurations
    /// </summary>
    public class GuildService : IServiceable
    {
        public IDatabase Database { get; }
        public GuildService(IDatabase database)
        {
            this.Database = database;
        }

        public Dictionary<ulong, GuildConfig> Cache = new Dictionary<ulong, GuildConfig>();

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

            var res = Database.Load<GuildConfig>(GuildConfig.DocumentName(guildId));
            if (res != null)
            {
                Cache.Add(guildId, res);
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
            Database.Store<GuildConfig>(config, GuildConfig.DocumentName(config.GuildId));
        }

        public class GuildConfig
        {
            public static string DocumentName(ulong guildId)
            {

                return $"GuildRavenConfig-{guildId}";
            }

            public ulong GuildId { get; set; }

            public bool UnknownCommandResponse { get; set; } = true;
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