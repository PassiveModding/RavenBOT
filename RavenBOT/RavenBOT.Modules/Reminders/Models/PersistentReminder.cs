using System;

namespace RavenBOT.Modules.Reminders.Models
{
    public class PersistentReminder
    {
        public static string DocumentName(ulong guildId, ulong userId, int reminderId)
        {
            return $"PersistentReminder-{guildId}-{userId}-{reminderId}";
        }

        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }

        public ulong ChannelId { get; set; }

        //Specifies time of day which the event will trigger
        //TODO: May require adjusting due to different timezones
        public TimeSpan ActivationTime { get; set; }

        public int GMTAdjustment { get; set; } = 0;

        public string ReminderMessage { get; set; }

        //NOTE: This is relevant to the userID not other reminders
        public int ReminderNumber { get; set; }
    }
}