using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RavenBOT.Modules.Lithium.Models.Moderation
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
        
        public ulong GuildId { get; }

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

        public bool UseBlacklist { get; set; }
        public List<BlacklistSet> Blacklist { get; set; } = new List<BlacklistSet>();
        public List<BlacklistSet.BlacklistMessage> BlacklistSimple { get; set; } = new List<BlacklistSet.BlacklistMessage>();

        public bool BlockInvites { get; set; }
        public bool BlockIps { get; set; }

        public bool BlockMassMentions { get; set; }
        public int MaxMentions { get; set; } = 5;
        public bool MassMentionsIncludeUsers { get; set; } = true;
        public bool MassMentionsIncludeRoles { get; set; } = true;
        public bool MassMentionsIncludeChannels { get; set; } = false;


        public bool UsePerspective { get; set; }
        public int PerspectiveMax { get; set; }
    }
}
