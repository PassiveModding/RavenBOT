using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace RavenBOT.Modules.AutoMod.Methods
{
    public partial class ModerationService
    {
        public enum SpamType
        {
            RepetitiveMessage,
            TooFast,
            None
        }

        public Dictionary<ulong, SpamGuild> SpamGuilds {get;set;} = new Dictionary<ulong, SpamGuild>();

        public class SpamGuild
        {
            public SpamGuild(ulong guildId)
            {
                GuildId = guildId;
            }
            public ulong GuildId {get;}
            public Dictionary<ulong, SpamChannel> SpamChannels = new Dictionary<ulong, SpamChannel>();
            public class SpamChannel
            {
                public SpamChannel(ulong channelId)
                {
                    ChannelId = channelId;
                }

                public ulong ChannelId {get;}

                public Dictionary<ulong, SpamUser> SpamUsers = new Dictionary<ulong, SpamUser>();

                public class SpamUser
                {
                    public SpamUser(ulong userId)
                    {
                        UserId = userId;
                    }

                    public ulong UserId {get;}

                    public List<SpamMessage> Messages {get; private set;} = new List<SpamMessage>();
                    public SpamType AddMessage(SocketUserMessage message, int maxMessages, int secondsCapture, int maxRepetitions, int cacheSize, out List<SpamMessage> messages)
                    {
                        var msg = new SpamMessage();
                        msg.MessageId = message.Id;
                        msg.Message = message.Content;

                        Messages.Add(msg);

                        var returnType = SpamType.None;
                        //Consider time based spam checks as well as count based spam checks
                        if (Messages.Count(x => x.TimeStamp > DateTime.UtcNow - TimeSpan.FromSeconds(secondsCapture)) >= maxMessages)
                        {
                            msg.Responded = true;
                            messages = Messages;
                            returnType = SpamType.TooFast;   
                        }
                        else if (Messages.GroupBy(x => x.Message).Max(x => x.Count()) >= maxRepetitions)
                        {
                            msg.Responded = true;
                            messages = Messages;
                            returnType = SpamType.RepetitiveMessage;
                        }
                        else
                        {
                            messages = null;
                        }

                        //This is done after `messages` is assigned to the reference when setting msg.Responded sets the item in the current list.
                        Messages = Messages.OrderByDescending(x => x.TimeStamp).Take(cacheSize).ToList();
                        return returnType;
                    }
                    public class SpamMessage
                    {
                        public string Message {get;set;}
                        public ulong MessageId {get;set;}
                        public DateTime TimeStamp {get;set;} = DateTime.UtcNow;
                        public bool Responded {get;set;} = false;
                        
                    }
                }
            }
        }

        public SpamType CheckSpam(SocketUserMessage message, SocketGuildChannel channel, out List<SpamGuild.SpamChannel.SpamUser.SpamMessage> messages)
        {
            SpamGuild guild;
            if (SpamGuilds.ContainsKey(channel.Guild.Id))
            {
                guild = SpamGuilds[channel.Guild.Id];
            }
            else
            {
                guild = new SpamGuild(channel.Guild.Id);
                SpamGuilds.Add(channel.Guild.Id, guild);
            }

            SpamGuild.SpamChannel spamChannel;
            if (guild.SpamChannels.ContainsKey(channel.Id))
            {
                spamChannel = guild.SpamChannels[channel.Id];
            }
            else
            {
                spamChannel = new SpamGuild.SpamChannel(channel.Id);
                guild.SpamChannels.Add(channel.Id, spamChannel);
            }

            SpamGuild.SpamChannel.SpamUser spamUser;
            if (spamChannel.SpamUsers.ContainsKey(message.Author.Id))
            {
                spamUser = spamChannel.SpamUsers[message.Author.Id];
            }
            else
            {
                spamUser = new SpamGuild.SpamChannel.SpamUser(message.Author.Id);
                spamChannel.SpamUsers.Add(message.Author.Id, spamUser);
            }

            var config = GetModerationConfig(channel.Guild.Id);

            var response = spamUser.AddMessage(message, config.SpamSettings.MessagesPerTime, config.SpamSettings.SecondsToCheck, config.SpamSettings.MaxRepititions, config.SpamSettings.CacheSize, out messages);
            return response;
        }
    }
}