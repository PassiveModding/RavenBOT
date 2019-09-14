using System.Threading.Tasks;
using RavenBOT.Common;

namespace RavenBOT.Migrations
{
    public class GuildServiceMigration : IServiceable
    {
        public GuildServiceMigration(IDatabase db, PrefixService pre, GuildService guild)
        {
            Db = db;
            Pre = pre;
            Guild = guild;
        }

        public IDatabase Db { get; }
        public PrefixService Pre { get; }
        public GuildService Guild { get; }

        /// <summary>
        /// Transfers stored prefixes from PrefixService to GuildService
        /// </summary>
        /// <returns></returns>
        public Task MigratePrefixes()
        {
            var currentDoc = Db.Load<PrefixService.PrefixInfo>(Pre.DocumentName);
            var defaultPrefix = Pre.DefaultPrefix;
            if (currentDoc != null)
            {
                foreach (var config in currentDoc.Prefixes)
                {
                    //Ignore if a config for the specified guild is already created.
                    if (Guild.GetConfig(config.Key) != null) continue;
                    //Ignore guilds that for whatever reason have the default prefix set.
                    if (config.Value.Equals(defaultPrefix, System.StringComparison.InvariantCultureIgnoreCase)) continue;

                    var newConfig = new GuildService.GuildConfig(config.Key);
                    newConfig.PrefixOverride = config.Value;
                    Guild.SaveConfig(newConfig);
                }
            }

            return Task.CompletedTask;
        }
    }
}