using System;
using System.Collections.Generic;

namespace RavenBOT.Modules.Moderator.Models
{
    public class ActionConfig
    {
        public static string DocumentName(ulong guildId)
        {
            return $"ActionConfig-{guildId}";
        }

        public ActionConfig(ulong guildId)
        {
            GuildId= guildId;
        }

        public ActionConfig() {}

        public ulong GuildId {get;set;}

        public enum Action
        {
            Kick,
            Ban,
            None
        }

        public int MaxWarnings {get;set;} = 5;

        public Action MaxWarningsAction {get;set;} = Action.Ban;

        public ulong MuteRole {get;set;}

        public class ActionUser
        {
            public static string DocumentName(ulong userId, ulong guildId)
            {
                return $"ModUser-{userId}-{guildId}";
            }

            public ActionUser(ulong userId, ulong guildId)
            {
                UserId = userId;
                GuildId = guildId;
            }

            public ActionUser(){}

            public ulong UserId {get;set;}
            public ulong GuildId {get;set;}

            public List<Warning> Warnings {get;set;} = new List<Warning>();

            public int WarnUser(ulong modId, string reason = null)
            {
                var warn = new Warning();
                warn.Warner = modId;
                warn.Reason = reason;

                Warnings.Add(warn);
                return Warnings.Count;
            }

            public class Warning
            {
                public string Reason {get;set;}

                //The ID of the user who issued the warning
                public ulong Warner {get;set;}

                public DateTime TimeStamp {get;set;} = DateTime.UtcNow;
            }
        }
    }
}