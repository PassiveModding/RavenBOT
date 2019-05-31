using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RavenBOT.Modules.AutoMod.Models.Moderation
{
    public class ModerationConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"Moderation-{guildId}";
        }

        public ModerationConfig(ulong guildId)
        {
            GuildId = guildId;
            PerspectiveMax = 95;
        }
        
        public ModerationConfig() { }

        public ulong GuildId { get; set; }

        //Indicates (matched?, matched value, regex?)
        public Tuple<bool, string, bool> BlacklistCheck(string message)
        {
            var match = BlacklistSimple.FirstOrDefault(x =>
            {
                if (x.Regex)
                {
                    return Regex.IsMatch(message, x.Content, RegexOptions.IgnoreCase);
                }

                return message.Contains(x.Content, StringComparison.InvariantCultureIgnoreCase);
            });

            if (match != null && match.Content != null)
            {
                return new Tuple<bool, string, bool>(true, match.Content, match.Regex);
            }

            var otherMatch = Blacklist.Select(x => x.CheckMatch(message)).FirstOrDefault(x => x.Item1);
            return otherMatch ?? new Tuple<bool, string, bool>(false, null, false);
        }

        public bool UseBlacklist { get; set; } = false;
        public List<BlacklistSet> Blacklist { get; set; } = new List<BlacklistSet>();
        public List<BlacklistSet.BlacklistMessage> BlacklistSimple { get; set; } = new List<BlacklistSet.BlacklistMessage>();

        public bool BlockInvites { get; set; } = false;
        public bool BlockIps { get; set; } = false;

        public bool BlacklistUsernames {get;set;} = false;
        public List<BlacklistSet.BlacklistMessage> BlacklistedUsernames {get;set;} = new List<BlacklistSet.BlacklistMessage>();

        public bool BlockMassMentions { get; set; } = false;
        public int MaxMentions { get; set; } = 5;
        public bool MassMentionsIncludeUsers { get; set; } = true;
        public bool MassMentionsIncludeRoles { get; set; } = true;
        public bool MassMentionsIncludeChannels { get; set; } = false;

        public bool UsePerspective { get; set; } = false;
        public int PerspectiveMax { get; set; } = 95;

        public bool UseAntiSpam {get;set;} = false;
        public AntiSpam SpamSettings {get;set;} = new AntiSpam();

        public List<ulong> AutoModExempt {get;set;} = new List<ulong>();

        public class AntiSpam
        {
            //Max messages to cache
            public int CacheSize {get; private set;} = 10;

            public bool SetCacheSize(int size)
            {
                if (size >= MessagesPerTime && size >= MaxRepititions)
                {
                    CacheSize = size;
                    return true;
                }

                return false;
            }

            //Seconds to check for timed spam
            public int SecondsToCheck {get;set;} = 10;

            //NOTE: Change setter to ensure amount is less than cache size.
            public int MessagesPerTime {get; private set;} = 10;

            public bool SetMaxMessagesPerTime(int max)
            {
                if (max <= CacheSize)
                {
                    SecondsToCheck = max;
                    return true;
                }

                return false;
            }

            public int MaxRepititions {get; private set;} = 5;

            public bool SetMaxRepititions(int max)
            {
                if (max <= CacheSize)
                {
                    MaxRepititions = max;
                    return true;
                }

                return false;
            }
        }
    }
}
