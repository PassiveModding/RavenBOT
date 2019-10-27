using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RavenBOT.Common
{
    public class ModuleManagementService
    {
        public ModuleManagementService(IDatabase database, PrefixService prefixService, bool dev)
        {
            throw new Exception("Module Management Service is depreciated. Use GuildService Instead.");
            Database = database;
            PrefixService = prefixService;
            Developer = dev;
        }

        public IDatabase Database { get; }
        public PrefixService PrefixService { get; }
        public bool Developer { get; }

        public class ModuleConfig
        {
            public static string DocumentName(ulong guildId)
            {
                return $"ModuleConfig-{guildId}";
            }

            public ModuleConfig(ulong guildId)
            {
                GuildId = guildId;
            }
            public ModuleConfig() { }

            public ulong GuildId { get; set; }
            public List<string> Blacklist { get; set; } = new List<string>();
        }

        public ModuleConfig GetModuleConfig(ulong guildId)
        {
            var setup = Database.Load<ModuleConfig>(ModuleConfig.DocumentName(guildId));
            if (setup == null)
            {
                setup = new ModuleConfig(guildId);
                Database.Store(setup, ModuleConfig.DocumentName(guildId));
            }

            return setup;
        }

        private bool IsAllowed(ulong guildId, string command)
        {
            //ignores blacklist if there is a non valid guild id
            if (guildId <= 0)
            {
                return true;
            }

            var config = GetModuleConfig(guildId);
            //Return if there is nothing in the blacklist
            if (!config.Blacklist.Any())
            {
                return true;
            }

            //Override prefix if the bot is in developer mode
            var prefix = Developer ? PrefixService.DefaultPrefix : PrefixService.GetPrefix(guildId);

            if (config.Blacklist.Any(x => command.StartsWith(x, true, CultureInfo.InvariantCulture) || command.StartsWith($"{prefix} {x}", true, CultureInfo.InvariantCulture) || command.StartsWith($"{prefix}{x}", true, CultureInfo.InvariantCulture)))
            {
                return false;
            }

            return true;
        }

        private void SaveModuleConfig(ModuleConfig config)
        {
            //Remove any empty blacklist items to try and avoid accidental blacklisting of all modules   
            //Also filter out duplicate entries         
            config.Blacklist = config.Blacklist.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            Database.Store(config, ModuleConfig.DocumentName(config.GuildId));
        }
    }
}