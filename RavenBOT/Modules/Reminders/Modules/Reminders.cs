using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Reminders.Methods;
using RavenBOT.Services.Database;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Reminders.Modules
{
    [Group("Reminder.")]
    [RequireContext(ContextType.Guild)]
    public class Reminders : InteractiveBase<ShardedCommandContext>
    {
        public ReminderHandler ReminderHandler {get;}

        public Reminders(IDatabase database, DiscordShardedClient client)
        {
            ReminderHandler = new ReminderHandler(database, client);
        }

        [Command("Remind")]
        [Alias("RemindMe")]
        public async Task Remind(TimeSpan length, [Remainder]string message)
        {
            ReminderHandler.AddReminder(new Models.Reminder(Context.Guild.Id, Context.User.Id, Context.Channel.Id, length, message));
            await ReplyAsync($"At {DateTime.UtcNow.ToLongDateString()} {DateTime.UtcNow.ToLongTimeString()} (in {length.GetReadableLength()}) I will remind you to: {message}");
        }
    }
}