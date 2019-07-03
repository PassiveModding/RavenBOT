using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.Modules.Reminders.Methods;
using RavenBOT.Modules.Reminders.Models;

namespace RavenBOT.Modules.Reminders.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class Reminders : InteractiveBase<ShardedCommandContext>
    {
        public ReminderHandler ReminderHandler { get; }

        public Reminders(ReminderHandler reminderHandler)
        {
            ReminderHandler = reminderHandler;
        }

        [Command("Remind")]
        [Alias("RemindMe")]
        [Summary("Schedules a reminder after the specified amount of time")]
        public async Task Remind(TimeSpan length, [Remainder] string message)
        {
            ReminderHandler.AddReminder(new Models.Reminder(Context.Guild.Id, Context.User.Id, Context.Channel.Id, length, message));
            await ReplyAsync("", false, $"At {DateTime.UtcNow.ToLongDateString()} {DateTime.UtcNow.ToLongTimeString()} (in {length.GetReadableLength()}) I will remind you to: {message}".QuickEmbed());
        }

        [Command("DailyReminder")]
        [Summary("Schedules a reminder that will run each day at the specified time")]
        public async Task Remind([Summary("This is in 24H time")] string timeOfDay, [Remainder] string message)
        {
            if (TimeSpan.TryParseExact(timeOfDay, "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan result))
            {
                var reminderWithNumber = ReminderHandler.AddReminder(new Models.PersistentReminder
                {
                    GuildId = Context.Guild.Id,
                        ChannelId = Context.Channel.Id,
                        UserId = Context.User.Id,
                        ReminderMessage = message,
                        ActivationTime = result,
                        //TODO: Get GMT Time
                });

                await ReplyAsync($"Reminder #{reminderWithNumber.ReminderNumber}, Current Time: {DateTime.UtcNow.ToShortTimeString()}", false, $"At {timeOfDay} each day I will remind you to: {message}".QuickEmbed());
            }
            else
            {
                await ReplyAsync("Invalid time format specified. Please enter it in 24H time with the format HH:mm");
                return;
            }
        }

        [Command("DeleteDailyReminder")]
        [Summary("Schedules a reminder that will run each day at the specified time")]
        public async Task DeleteDailyReminder(int reminderNumber)
        {
            var reminder = ReminderHandler.Database.Load<PersistentReminder>(PersistentReminder.DocumentName(Context.Guild.Id, Context.User.Id, reminderNumber));
            if (reminder == null)
            {
                await ReplyAsync("Invalid reminder number specified.");
                return;
            }

            ReminderHandler.RemoveReminder(reminder);
            await ReplyAsync("Reminder removed.");
        }

        [Command("DailyReminderOffset")]
        [Summary("Sets the GMT time offset for your daily reminder")]
        public async Task SetGMTOffset(int reminderNumber, int offset)
        {
            var reminder = ReminderHandler.Database.Load<PersistentReminder>(PersistentReminder.DocumentName(Context.Guild.Id, Context.User.Id, reminderNumber));
            if (reminder == null)
            {
                await ReplyAsync("Invalid reminder number specified.");
                return;
            }

            reminder.GMTAdjustment = offset;

            var cachedReminder = ReminderHandler.PersistentReminders.FirstOrDefault(x => x.Reminder.UserId == Context.User.Id && x.Reminder.ReminderNumber == reminderNumber);
            cachedReminder.Reminder.GMTAdjustment = offset;
            cachedReminder.Timer.Dispose();
            cachedReminder.Timer = new Timer(x => ReminderHandler.ExecutePersistentReminder(cachedReminder.Reminder), null, ReminderHandler.GetTimerFirstRunTime(cachedReminder.Reminder), new TimeSpan(24, 0, 0));
            ReminderHandler.Database.Store(reminder, PersistentReminder.DocumentName(Context.Guild.Id, Context.User.Id, reminder.ReminderNumber));
            await ReplyAsync("Your GMT adjustment has been set for the specified reminder.");
        }

        [Command("DailyReminders")]
        [Summary("Displays all your current daily reminders")]
        public async Task DailyReminderLookup()
        {
            var reminders = ReminderHandler.Database.Query<PersistentReminder>(x => x.UserId == Context.User.Id);
            if (!reminders.Any())
            {
                await ReplyAsync("You have no reminders.");
                return;
            }

            var pager = new PaginatedMessage();
            pager.Pages = reminders.Select(x =>
                new PaginatedMessage.Page
                {
                    Description = x.ReminderMessage,
                        Title = $"Reminder: #{x.ReminderNumber} sent in {Context.Client.GetGuild(x.GuildId)?.GetTextChannel(x.ChannelId)?.Name ?? "DMs"}",
                        FooterOverride = new Discord.EmbedFooterBuilder
                        {
                            Text = $"At {x.ActivationTime.ToString("hh\\:mm")} daily {GetGMTString(x.GMTAdjustment)}"
                        }
                }).ToList();
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                    Backward = true,
                    Trash = true
            });
        }

        public string GetGMTString(int offset)
        {
            if (offset == 0)
            {
                return "";
            }
            else if (offset > 0)
            {
                return $"(GMT+{offset})";
            }
            else
            {
                return $"(GMT{offset})";
            }
        }
    }
}