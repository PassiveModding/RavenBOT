using System;
using System.Collections.Generic;
using System.Linq;

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
            GuildId = guildId;
        }

        public ActionConfig() {}

        public ulong GuildId { get; set; }

        public enum Action
        {
            Kick,
            Ban,
            Mute,
            SoftBan,
            None
        }

        public int MaxWarnings { get; set; } = 5;

        public Action MaxWarningsAction { get; set; } = Action.Ban;

        public ulong MuteRole { get; set; }

        public int AddLogAction(ulong user, ulong moderator, Log.LogAction action, string reason = null, TimeSpan? length = null)
        {
            var id = LogActions.Any() ? LogActions.Max(x => x.CaseId) + 1 : 1;
            var newAction = new Log(user, moderator, action, id, reason, length);
            LogActions.Add(newAction);
            return id;
        }

        public TimeSpan SoftBanLength { get; set; } = TimeSpan.FromHours(24);

        public TimeSpan MuteLength { get; set; } = TimeSpan.FromHours(1);

        public ulong LogChannelId { get; set; }

        public List<Log> LogActions { get; set; } = new List<Log>();

        public class Log
        {
            public Log(ulong user, ulong moderator, LogAction action, int id, string reason = null, TimeSpan? length = null)
            {
                Target = user;
                Moderator = moderator;
                Action = action;
                CaseId = id;
                Reason = reason;
                TimeStamp = DateTime.UtcNow;
                Duration = length;
            }

            public Log() {}

            public ulong Target { get; set; }
            public ulong Moderator { get; set; }
            public string Reason { get; set; }
            public int CaseId { get; set; }
            public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

            public LogAction Action { get; set; }

            public TimeSpan? Duration { get; set; } = null;

            public enum LogAction
            {
                Ban,
                Kick,
                Warn,
                Mute,
                SoftBan
            }
        }

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

            public ActionUser() {}

            public ulong UserId { get; set; }
            public ulong GuildId { get; set; }

            public List<Warning> Warnings { get; set; } = new List<Warning>();

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
                public string Reason { get; set; }

                //The ID of the user who issued the warning
                public ulong Warner { get; set; }

                public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
            }
        }
    }
}