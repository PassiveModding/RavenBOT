using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RavenBOT.Services.Database;

namespace RavenBOT.Services
{
    public class ModuleManagementService
    {
        public ModuleManagementService(IDatabase database)
        {
            Database = database;
        }

        public IDatabase Database { get; }

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
            public ModuleConfig(){}

            public ulong GuildId {get;set;}
            public List<string> Blacklist {get;set;} = new List<string>();
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

        public bool IsAllowed(ulong guildId, string command)
        {
            if (guildId <= 0)
            {
                return true;
            }

            var config = GetModuleConfig(guildId);
            if (!config.Blacklist.Any())
            {
                return true;
            }

            if (config.Blacklist.Any(x => command.StartsWith(x, true, CultureInfo.InvariantCulture)))
            {
                return false;
            }

            return true;
        }

        public void SaveModuleConfig(ModuleConfig config)
        {
            //Remove any empty blacklist items to try and avoid accidental blacklisting of all modules   
            //Also filter out duplicate entries         
            config.Blacklist = config.Blacklist.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            Database.Store(config, ModuleConfig.DocumentName(config.GuildId));
        }
    }
}