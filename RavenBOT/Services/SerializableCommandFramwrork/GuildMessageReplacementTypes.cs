using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.Commands;

namespace RavenBOT.Services.SerializableCommandFramwrork
{
    public class GuildMessageReplacementTypes
    {
        public interface IGuildMessageReplacementType
        {
            string Value { get; set; }

            string ReplacementValue(SocketCommandContext context);
        }

        public List<IGuildMessageReplacementType> GetIGuildMessageReplacementTypes()
        {
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IGuildMessageReplacementType)))
                .Select((t, i) => Activator.CreateInstance(t) as IGuildMessageReplacementType);
            return tests.ToList();
        }

        public class GuildName : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Name;
            }
        }

        public class GuildId : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildId";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Id.ToString();
            }
        }

        public class GuildOwnerDisplayName : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildOwnerDisplayName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Owner.Nickname ?? context.Guild.Owner.Username;
            }
        }

        public class GuildownerUserName : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildOwnerUserName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Owner.Username;
            }
        }

        public class GuildOwnerId : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildOwnerId";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.OwnerId.ToString();
            }
        }

        public class GuildIconUrl : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildIconUrl";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.IconUrl;
            }
        }

        public class GuildChannelNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Channels.Select(x => x.Name));
            }
        }

        
        public class GuildChannelIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Channels.Select(x => x.Id));
            }
        }

        
        public class GuildTextChannelNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildTextChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.TextChannels.Select(x => x.Name));
            }
        }

        public class GuildTextChannelIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildTextChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.TextChannels.Select(x => x.Id));
            }
        }

        
        public class GuildVoiceChannelNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildVoiceChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.VoiceChannels.Select(x => x.Name));
            }
        }

        public class GuildVoiceChannelIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildVoiceChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.VoiceChannels.Select(x => x.Id));
            }
        }
        
        public class GuildCategoryChannelNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildCategoryChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.CategoryChannels.Select(x => x.Name));
            }
        }

        public class GuildCategoryChannelIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildCategoryChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.CategoryChannels.Select(x => x.Id));
            }
        }

        public class GuildRoleNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildRoleNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Roles.Select(x => x.Name));
            }
        }

        public class GuildRoleIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildRoleIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Roles.Select(x => x.Id));
            }
        }

        public class GuildEmoteUrls : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildEmoteUrls";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Url));
            }
        }

        public class GuildEmoteIds : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildEmoteIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Id));
            }
        }

        
        public class GuildEmoteNames : IGuildMessageReplacementType
        {
            public string Value { get; set; } = "guildEmoteNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Name));
            }
        }
    }
}
