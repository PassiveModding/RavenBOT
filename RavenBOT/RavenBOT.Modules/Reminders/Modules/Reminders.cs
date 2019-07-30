using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
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

        [Command("Reminders")]
        public async Task RemindersListAsync()
        {
            var reminders = ReminderHandler.Database.Query<Reminder>(x => x.UserId == Context.User.Id).ToArray();
            if (reminders.Length == 0)
            {
                await ReplyAsync("There are no reminders in queue for you.");
                return;
            }
            var currentServer = reminders.Where(x => x.GuildId == Context.Guild.Id).ToArray();
            var currentChannel = currentServer.Where(x => x.ChannelId == Context.Channel.Id).ToArray();

            var pager = new PaginatedMessage();
            var p1 = new PaginatedMessage.Page
            {
                Title = "Overview",
                Description = $"**Total Reminders:** {reminders.Length}\n" +
                $"**Total Reminders in this Server:** {currentServer.Length}\n" +
                $"**Total Reminders in this Channel:** {currentChannel.Length}\n"
            };

            var pages = new List<PaginatedMessage.Page>
            {
                p1
            };

            var fields = reminders.Select(x =>
            {
                var dueAt = (x.TimeStamp + x.Length);
                var guild = Context.Client.GetGuild(x.GuildId);
                var channel = guild?.GetTextChannel(x.ChannelId);
                return new EmbedFieldBuilder
                {
                    Name = $"Reminder: {x.ReminderNumber}",
                        Value = $"**At:** {dueAt.ToLongDateString()} {dueAt.ToLongTimeString()}\n" +
                        $"**Due In:** {(dueAt - DateTime.UtcNow).GetReadableLength()}\n" +
                        $"**Server:** {guild?.Name ?? "N/A"}\n" +
                        $"**Channel:** {channel?.Name ?? "N/A"}\n**Message:**\n" +
                        $"{x.ReminderMessage}"
                };
            }).SplitList(5).ToList();

            if (fields.Count > 1)
            {
                p1.Fields = fields.First().ToList();
                foreach (var grp in fields.Skip(1))
                {
                    pages.Add(new PaginatedMessage.Page
                    {
                        Fields = grp.ToList()
                    });
                }
            }
            else if (fields.Count == 1)
            {
                p1.Fields = fields.First().ToList();
            }

            pager.Pages = pages;

            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                    Backward = true
            });
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