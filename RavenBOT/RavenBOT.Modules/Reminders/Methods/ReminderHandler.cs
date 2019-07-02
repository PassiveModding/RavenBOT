using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Modules.Reminders.Models;

namespace RavenBOT.Modules.Reminders.Methods
{
    public class ReminderHandler : IServiceable
    {
        public IDatabase Database { get; }
        private DiscordShardedClient Client { get; }
        public LocalManagementService LocalManagementService { get; }
        private System.Threading.Timer Timer { get; set; }

        private List<Reminder> Reminders { get; set; }

        public List<PersistentReminderTimer> PersistentReminders { get; set; }

        public class PersistentReminderTimer
        {
            public PersistentReminder Reminder { get; set; }
            public Timer Timer { get; set; }
        }

        public TimeSpan GetTimerFirstRunTime(PersistentReminder reminder)
        {
            var day = new TimeSpan(24, 0, 0);
            var now = TimeSpan.Parse(DateTime.UtcNow.ToString("HH:mm"));
            TimeSpan timeLeftUntilFirstRun = (day - now) + reminder.ActivationTime + TimeSpan.FromHours(reminder.GMTAdjustment);
            if (timeLeftUntilFirstRun.TotalHours > 24)
                timeLeftUntilFirstRun -= day;
            return timeLeftUntilFirstRun;
        }

        public PersistentReminderTimer MakeTimer(PersistentReminder reminder)
        {
            return new PersistentReminderTimer
            {
                Reminder = reminder,
                    Timer = new Timer(c => ExecutePersistentReminder(reminder), null, GetTimerFirstRunTime(reminder), new TimeSpan(24, 00, 00))
            };
        }

        public ReminderHandler(IDatabase database, ShardChecker checker, DiscordShardedClient client, LocalManagementService localManagementService)
        {
            Database = database;
            Client = client;
            LocalManagementService = localManagementService;
            Reminders = Database.Query<Reminder>().ToList();
            PersistentReminders = Database.Query<PersistentReminder>().Select(MakeTimer).ToList();

            checker.AllShardsReady += () =>
            {
                Timer = new System.Threading.Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                return Task.CompletedTask;
            };
        }
        public void ExecutePersistentReminder(PersistentReminder reminder)
        {
            var _ = Task.Run(async() =>
            {
                if (!LocalManagementService.LastConfig.IsAcceptable(reminder.GuildId))
                {
                    return;
                }

                var channel = Client.GetGuild(reminder.GuildId)?.GetTextChannel(reminder.ChannelId);
                var user = Client.GetUser(reminder.UserId);
                if (user == null)
                {
                    //Filter out any reminders that contain this user.
                    PersistentReminders = PersistentReminders.Where(x =>
                    {
                        if (x.Reminder.UserId != reminder.UserId)
                        {
                            return true;
                        }
                        else
                        {
                            x.Timer.Dispose();
                            return false;
                        }
                    }).ToList();
                    return;
                }

                if (channel == null)
                {
                    //DM User if available.
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    if (dmChannel != null)
                    {
                        await dmChannel.SendMessageAsync(reminder.ReminderMessage).ConfigureAwait(false);
                    }
                    return;
                }

                await channel.SendMessageAsync($"{user.Mention}", false, new EmbedBuilder()
                {
                    Description = $"{reminder.ReminderMessage}".FixLength(1024),
                        Color = Color.Green
                }.Build()).ConfigureAwait(false);
            });
        }

        //NOTE: Reminder ID is discarded and re-set here
        public void AddReminder(Reminder reminder)
        {
            reminder.ReminderNumber = Reminders.Any() ? Reminders.Max(x => x.ReminderNumber) + 1 : 1;
            Reminders.Add(reminder);
            Database.Store(reminder, Reminder.DocumentName(reminder.GuildId, reminder.ReminderNumber));
        }

        public PersistentReminder AddReminder(PersistentReminder reminder)
        {
            reminder.ReminderNumber = PersistentReminders.Any() ? PersistentReminders.Where(x => x.Reminder.UserId == reminder.UserId).Max(x => x.Reminder.ReminderNumber) + 1 : 1;
            PersistentReminders.Add(MakeTimer(reminder));
            Database.Store(reminder, Reminder.DocumentName(reminder.GuildId, reminder.ReminderNumber));
            return reminder;
        }

        public void RemoveReminder(PersistentReminder reminder)
        {
            PersistentReminders = PersistentReminders.Where(x =>
            {
                if (x.Reminder.UserId == reminder.UserId && x.Reminder.ReminderNumber == reminder.ReminderNumber)
                {
                    x.Timer.Dispose();
                    return false;
                }

                return true;
            }).ToList();
            Database.Remove<PersistentReminder>(PersistentReminder.DocumentName(reminder.GuildId, reminder.UserId, reminder.ReminderNumber));
        }

        public void RemoveReminder(Reminder reminder)
        {
            try
            {
                Database.Remove<Reminder>(Reminder.DocumentName(reminder.GuildId, reminder.ReminderNumber));
                Reminders.Remove(reminder);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public bool TimerWait { get; set; } = false;

        public void TimerEvent(object _)
        {
            //Don't allow multiple to run at the same time and generate possible race conditions
            if (TimerWait)
            {
                return;
            }

            TimerWait = true;

            //TODO: Check that bot is actually logged in before running
            try
            {
                foreach (var reminder in Reminders.ToList())
                {
                    if (reminder.TimeStamp + reminder.Length < DateTime.UtcNow)
                    {
                        if (!LocalManagementService.LastConfig.IsAcceptable(reminder.GuildId))
                        {
                            return;
                        }

                        var channel = Client.GetGuild(reminder.GuildId)?.GetTextChannel(reminder.ChannelId);
                        var user = Client.GetUser(reminder.UserId);
                        if (user == null)
                        {
                            RemoveReminder(reminder);
                            continue;
                        }

                        if (channel == null)
                        {
                            //DM User if available.
                            var dmChannel = user.GetOrCreateDMChannelAsync().Result;
                            if (dmChannel != null)
                            {
                                //TODO: Pretty up the message
                                dmChannel.SendMessageAsync(reminder.ReminderMessage).ConfigureAwait(false);
                            }
                            RemoveReminder(reminder);
                            continue;
                        }

                        channel.SendMessageAsync($"{user.Mention}", false, new EmbedBuilder()
                        {
                            Description = $"{reminder.ReminderMessage}".FixLength(1024),
                                Color = Color.Green
                        }.Build()).ConfigureAwait(false);
                        RemoveReminder(reminder);
                    }
                }
            }
            finally
            {
                TimerWait = false;
            }
        }
    }
}