using RavenBOT.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RavenBOT.Migrations
{
    public class GuildServiceMigration : IServiceable
    {
        public GuildServiceMigration(IDatabase db, GuildService guild)
        {
            Db = db;
            Guild = guild;
        }

        public IDatabase Db { get; }
        public GuildService Guild { get; }

        /// <summary>
        /// Performs both module and prefix migrations in one operation
        /// </summary>
        /// <returns></returns>
        public Task RunMigration()
        {
            var prefixInfo = Db.Load<PrefixService.PrefixInfo>(PrefixService.DocumentName);
            var configs = new List<GuildService.GuildConfig>();
            if (prefixInfo != null)
            {
                foreach (var config in prefixInfo.Prefixes)
                {
                    //Ignore configs that just use the default prefix.
                    if (config.Value.Equals(Guild.Config.Prefix, System.StringComparison.InvariantCultureIgnoreCase)) continue;

                    var newConfig = new GuildService.GuildConfig(config.Key);
                    newConfig.PrefixOverride = config.Value;
                    configs.Add(newConfig);
                }
            }

            var moduleConfigs = Db.Query<ModuleManagementService.ModuleConfig>();
            foreach (var config in moduleConfigs)
            {
                //Ignore empty blacklists
                if (config.Blacklist.Count == 0) continue;

                var match = configs.FirstOrDefault(x => x.GuildId == config.GuildId);
                if (match == null)
                {
                    match = new GuildService.GuildConfig(config.GuildId);
                    configs.Add(match);
                }

                foreach (var module in config.Blacklist)
                {
                    match.ModuleBlacklist.Add(module);
                }
            }

            Db.StoreMany(configs, x => GuildService.GuildConfig.DocumentName(x.GuildId));
            return Task.CompletedTask;
        }


        /// <summary>
        /// Transfers stored prefixes from PrefixService to GuildService
        /// </summary>
        /// <returns></returns>
        public Task MigratePrefixes(PrefixService.PrefixInfo info, bool forced)
        {
            var currentDoc = Db.Load<PrefixService.PrefixInfo>(PrefixService.DocumentName);
            var defaultPrefix = Guild.Config.Prefix;
            if (currentDoc != null)
            {
                foreach (var config in currentDoc.Prefixes)
                {
                    //Ignore if a config for the specified guild is already created.
                    //NOTE: If forced is true then it will create a new config and save it regardless
                    if (!forced && Guild.GetConfig(config.Key) != null) continue;
                    //Ignore guilds that for whatever reason have the default prefix set.
                    if (config.Value.Equals(defaultPrefix, System.StringComparison.InvariantCultureIgnoreCase)) continue;

                    var newConfig = new GuildService.GuildConfig(config.Key);
                    newConfig.PrefixOverride = config.Value;
                    Guild.SaveConfig(newConfig);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Transfers stored ModuleManagement configs over to GuildConfigs
        /// </summary>
        /// <param name="forced"></param>
        /// <returns></returns>
        public Task MigrateModules(bool forced)
        {
            var currentDoc = Db.Query<ModuleManagementService.ModuleConfig>();
            foreach (var config in currentDoc)
            {
                if (!forced && Guild.GetConfig(config.GuildId) != null) continue;

                var newConfig = new GuildService.GuildConfig(config.GuildId);
                foreach (var module in config.Blacklist)
                {
                    newConfig.ModuleBlacklist.Add(module);
                }
                Guild.SaveConfig(newConfig);
            }

            return Task.CompletedTask;
        }
    }
}