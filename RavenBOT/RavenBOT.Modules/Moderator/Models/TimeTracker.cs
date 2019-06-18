using System;
using System.Collections.Generic;

namespace RavenBOT.Modules.Moderator.Models
{
    public class TimeTracker
    {
        public static string DocumentName { get; set; } = "TimedModerations";

        public List<User> Users { get; set; } = new List<User>();

        public class User
        {
            public User(ulong userId, ulong guildId, TimedAction action, TimeSpan length)
            {
                UserId = userId;
                GuildId = guildId;
                Length = length;
                Action = action;
            }

            public User() {}

            public enum TimedAction
            {
                SoftBan,
                Mute
            }

            public TimedAction Action { get; set; }

            public ulong UserId { get; set; }
            public ulong GuildId { get; set; }

            public TimeSpan Length { get; set; }

            public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        }
    }
}