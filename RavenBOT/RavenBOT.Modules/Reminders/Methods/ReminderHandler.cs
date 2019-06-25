using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Modules.Reminders.Models;

namespace RavenBOT.Modules.Reminders.Methods
{
    public class ReminderHandler : IServiceable
    {
        private IDatabase Database { get; }
        private DiscordShardedClient Client { get; }
        public LocalManagementService LocalManagementService { get; }
        private Timer Timer { get; set; }

        private List<Reminder> Reminders { get; set; }

        public ReminderHandler(IDatabase database, ShardChecker checker, DiscordShardedClient client, LocalManagementService localManagementService)
        {
            Database = database;
            Client = client;
            LocalManagementService = localManagementService;
            Reminders = Database.Query<Reminder>().ToList();
            checker.AllShardsReady += () =>
            {
                Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                return Task.CompletedTask;
            };
        }

        //NOTE: Reminder ID is discarded and re-set here
        public void AddReminder(Reminder reminder)
        {
            reminder.ReminderNumber = Reminders.Any() ? Reminders.Max(x => x.ReminderNumber) + 1 : 1;;
            Reminders.Add(reminder);
            Database.Store(reminder, Reminder.DocumentName(reminder.GuildId, reminder.ReminderNumber));
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