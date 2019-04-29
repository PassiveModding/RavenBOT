using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.Services.SerializableCommandFramework
{
    public class GuildMessageReplacementTypes
    {
        public interface IContextReplacement
        {
            string Value { get; set; }
           
            string ReplacementValue(SocketCommandContext context);
        }

        public interface ITextChannelReplacement
        {
            string Value { get; set; }

            string ReplacementValue(SocketTextChannel channel);
        }
        
        public static string DoReplacements(string message, SocketCommandContext context, string textChannelId = null)
        {
            foreach (var messageReplacementType in GetIGuildMessageReplacementTypes())
            {
                if (message.Contains($"{{{messageReplacementType.Value}}}"))
                {
                    message = message.Replace($"{{{messageReplacementType.Value}}}", messageReplacementType.ReplacementValue(context));
                }
            }


            //Note that channels will be limited to the current guild and will NOT be able to modify or contact any channels outside it.
            if (textChannelId != null)
            {
                if (!string.IsNullOrWhiteSpace(textChannelId))
                {
                    if (ulong.TryParse(textChannelId, out var chanId))
                    {
                        var textChannel = context.Guild.TextChannels.FirstOrDefault(x => x.Id == chanId);
                        if (textChannel != null)
                        {
                            foreach (var messageReplacementType in GetIChannelMessageReplacementTypes())
                            {
                                if (message.Contains($"{{{messageReplacementType.Value}}}"))
                                {
                                    message = message.Replace($"{{{messageReplacementType.Value}}}", messageReplacementType.ReplacementValue(textChannel));
                                }
                            }
                        }
                    }
                }
            }

            return message;
        }

        public class ChannelName : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelName";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.Name;
            }
        }

        public class ChannelTopic : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelTopic";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.Topic ?? "";
            }
        }

        public class ChannelMention : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelMention";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.Mention;
            }
        }

        public class ChannelNsfw : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelIsNsfw";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.IsNsfw.ToString();
            }
        }

        public class ChannelCreationDateTime : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelCreationDateTime";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.CreatedAt.DateTime.ToShortDateString() + " " + channel.CreatedAt.DateTime.ToShortTimeString();
            }
        }

        public class ChannelCreationDate : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelCreationDate";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.CreatedAt.DateTime.ToShortDateString();
            }
        }

        public class ChannelCreationTime : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelCreationTime";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.CreatedAt.DateTime.ToShortTimeString();
            }
        }

        public class ChannelUsers : ITextChannelReplacement
        {
            public string Value { get; set; } = "channelMembers";
            public string ReplacementValue(SocketTextChannel channel)
            {
                return channel.Users.Count.ToString();
            }
        }

        public static List<IContextReplacement> GetIGuildMessageReplacementTypes()
        {
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IContextReplacement)))
                .Select((t, i) => Activator.CreateInstance(t) as IContextReplacement);
            return tests.ToList();
        }

        
        public static List<ITextChannelReplacement> GetIChannelMessageReplacementTypes()
        {
            var tests = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ITextChannelReplacement)))
                .Select((t, i) => Activator.CreateInstance(t) as ITextChannelReplacement);
            return tests.ToList();
        }

        public class GuildName : IContextReplacement
        {
            public string Value { get; set; } = "guildName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Name;
            }
        }

        public class GuildId : IContextReplacement
        {
            public string Value { get; set; } = "guildId";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Id.ToString();
            }
        }

        public class CurrentChannelId : IContextReplacement
        {
            public string Value { get; set; } = "currentChannelId";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Channel.Id.ToString();
            }
        }

        public class CurrentChannelName : IContextReplacement
        {
            public string Value { get; set; } = "currentChannelName";

            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Channel.Name;
            }
        }

        public class CurrentUserUsername : IContextReplacement
        {
            public string Value { get; set; } = "currentUserUsername";

            public string ReplacementValue(SocketCommandContext context)
            {
                return context.User.Username;
            }
        }

        public class CurrentUserMention : IContextReplacement
        {
            public string Value { get; set; } = "currentUserMention";

            public string ReplacementValue(SocketCommandContext context)
            {
                return context.User.Mention;
            }
        }

        public class CurrentUserId : IContextReplacement
        {
            public string Value { get; set; } = "currentUserId";

            public string ReplacementValue(SocketCommandContext context)
            {
                return context.User.Id.ToString();
            }
        }

        public class GuildOwnerDisplayName : IContextReplacement
        {
            public string Value { get; set; } = "guildOwnerDisplayName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Owner.Nickname ?? context.Guild.Owner.Username;
            }
        }

        public class GuildownerUserName : IContextReplacement
        {
            public string Value { get; set; } = "guildOwnerUserName";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.Owner.Username;
            }
        }

        public class GuildOwnerId : IContextReplacement
        {
            public string Value { get; set; } = "guildOwnerId";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.OwnerId.ToString();
            }
        }

        public class GuildIconUrl : IContextReplacement
        {
            public string Value { get; set; } = "guildIconUrl";
            public string ReplacementValue(SocketCommandContext context)
            {
                return context.Guild.IconUrl;
            }
        }

        public class GuildChannelNames : IContextReplacement
        {
            public string Value { get; set; } = "guildChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Channels.Select(x => x.Name));
            }
        }

        
        public class GuildChannelIds : IContextReplacement
        {
            public string Value { get; set; } = "guildChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Channels.Select(x => x.Id));
            }
        }

        
        public class GuildTextChannelNames : IContextReplacement
        {
            public string Value { get; set; } = "guildTextChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.TextChannels.Select(x => x.Name));
            }
        }

        public class GuildTextChannelIds : IContextReplacement
        {
            public string Value { get; set; } = "guildTextChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.TextChannels.Select(x => x.Id));
            }
        }

        
        public class GuildVoiceChannelNames : IContextReplacement
        {
            public string Value { get; set; } = "guildVoiceChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.VoiceChannels.Select(x => x.Name));
            }
        }

        public class GuildVoiceChannelIds : IContextReplacement
        {
            public string Value { get; set; } = "guildVoiceChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.VoiceChannels.Select(x => x.Id));
            }
        }
        
        public class GuildCategoryChannelNames : IContextReplacement
        {
            public string Value { get; set; } = "guildCategoryChannelNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.CategoryChannels.Select(x => x.Name));
            }
        }

        public class GuildCategoryChannelIds : IContextReplacement
        {
            public string Value { get; set; } = "guildCategoryChannelIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.CategoryChannels.Select(x => x.Id));
            }
        }

        public class GuildRoleNames : IContextReplacement
        {
            public string Value { get; set; } = "guildRoleNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Roles.Select(x => x.Name));
            }
        }

        public class GuildRoleIds : IContextReplacement
        {
            public string Value { get; set; } = "guildRoleIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Roles.Select(x => x.Id));
            }
        }

        public class GuildEmoteUrls : IContextReplacement
        {
            public string Value { get; set; } = "guildEmoteUrls";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Url));
            }
        }

        public class GuildEmoteIds : IContextReplacement
        {
            public string Value { get; set; } = "guildEmoteIds";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Id));
            }
        }

        
        public class GuildEmoteNames : IContextReplacement
        {
            public string Value { get; set; } = "guildEmoteNames";
            public string ReplacementValue(SocketCommandContext context)
            {
                return string.Join(", ", context.Guild.Emotes.Select(x => x.Name));
            }
        }
    }
}
