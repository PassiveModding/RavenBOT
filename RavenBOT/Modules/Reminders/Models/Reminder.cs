using System;

namespace RavenBOT.Modules.Reminders.Models
{
    public class Reminder
    {
        public static string DocumentName(ulong guildId, int reminderId)
        {
            return $"Reminder-{guildId}-{reminderId}";
        }

        public Reminder(ulong guildId, ulong userId, ulong channelId, TimeSpan length, string message)
        {
            GuildId = guildId;
            UserId = userId;
            Length = length;
            TimeStamp = DateTime.UtcNow;
            ChannelId = channelId;
            ReminderMessage = message;
        }

        public Reminder(ulong guildId, ulong userId, ulong channelId, DateTime date, string message)
        {
            GuildId = guildId;
            UserId = userId;
            ChannelId = channelId;
            Length = date - DateTime.UtcNow;
            TimeStamp = DateTime.UtcNow;
            ReminderMessage = message;
        }

        public Reminder() {}

        public ulong GuildId {get;set;}
        public ulong UserId {get;set;}

        public ulong ChannelId {get;set;}
        public DateTime TimeStamp {get;set;}

        public TimeSpan Length {get;set;}

        public string ReminderMessage {get;set;}

        public int ReminderId {get;set;}
    }
}