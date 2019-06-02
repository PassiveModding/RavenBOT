using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Modules.Reminders.Methods;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Reminders.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Reminders : InteractiveBase<ShardedCommandContext>
    {
        public ReminderHandler ReminderHandler {get;}

        public Reminders(ReminderHandler reminderHandler)
        {
            ReminderHandler = reminderHandler;
        }

        [Command("Remind")]
        [Alias("RemindMe")]
        [Summary("Schedules a reminder after the specified amount of time")]
        public async Task Remind(TimeSpan length, [Remainder]string message)
        {
            ReminderHandler.AddReminder(new Models.Reminder(Context.Guild.Id, Context.User.Id, Context.Channel.Id, length, message));
            await ReplyAsync($"At {DateTime.UtcNow.ToLongDateString()} {DateTime.UtcNow.ToLongTimeString()} (in {length.GetReadableLength()}) I will remind you to: {message}");
        }
    }
}