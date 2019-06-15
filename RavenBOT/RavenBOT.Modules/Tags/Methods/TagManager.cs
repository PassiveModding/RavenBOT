using RavenBOT.Modules.Tags.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Tags.Methods
{
    public class TagManager : IServiceable
    {
        public TagManager(IDatabase database)
        {
            Database = database;
        }

        public IDatabase Database { get; }

        public TagGuild GetTagGuild(ulong guildId)
        {
            var config = Database.Load<TagGuild>(TagGuild.DocumentName(guildId));
            if (config == null)
            {
                config = new TagGuild(guildId);
                Database.Store(config, TagGuild.DocumentName(guildId));
            }

            return config;
        }

        public void SaveTagGuild(TagGuild config)
        {
            Database.Store(config, TagGuild.DocumentName(config.GuildId));
        }
    }
}