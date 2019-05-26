using System.Collections.Generic;

namespace RavenBOT.Modules.Tags.Models
{
    public class TagGuild
    {
        public static string DocumentName(ulong guildId)
        {
            return $"TagGuild-{guildId}";
        }

        public TagGuild(ulong guildId)
        {
            GuildId = guildId;
        }

        public TagGuild(){}

        public ulong GuildId {get;set;}

        public List<Tag> Tags {get;set;} = new List<Tag>();
        public class Tag
        {
            public Tag(ulong creatorId, string name, string response)
            {
                Creator = creatorId;
                Name = name;
                Response = response;
            }

            public ulong Creator {get;set;}

            public string Name {get;set;}
            public string Response {get;set;}

            public int Hits {get;set;} = 0;

            //TODO: Implement configurable embed response?
            //TODO: Possibly implement prefix-less tags, ie. will respond if a user sends a message starting with the tag name.
        }
    }
}