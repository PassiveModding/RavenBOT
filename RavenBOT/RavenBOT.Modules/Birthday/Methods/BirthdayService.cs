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
using RavenBOT.Modules.Birthday.Models;

namespace RavenBOT.Modules.Birthday.Methods
{
    public class BirthdayService : IServiceable
    {
        public DiscordShardedClient Client { get; }
        public IDatabase Database { get; }
        public LocalManagementService LocalManagementService { get; }
        public Timer Timer { get; set; }

        public bool Running { get; set; } = false;

        public BirthdayService(DiscordShardedClient client, ShardChecker checker, IDatabase database, LocalManagementService localManagementService)
        {
            Client = client;
            Database = database;
            LocalManagementService = localManagementService;
            checker.AllShardsReady += () =>
            {
                Timer = new Timer(TimerEvent, null, TimeSpan.FromMinutes(0), TimeSpan.FromHours(1));
                return Task.CompletedTask;
            };
        }

        public void TimerEvent(object _)
        {
            var t = Task.Run(() => NotifyGuilds());
        }

        public List<BirthdayModel> GetCurrentBirthdays()
        {
            var birthdays = Database.Query<BirthdayModel>()?.Where(x => x.IsToday()).ToList() ?? new List<BirthdayModel>();
            return birthdays;
        }

        public async Task NotifyGuilds()
        {
            if (Running)
            {
                return;
            }

            try
            {
                Running = true;
                var guilds = Database.Query<BirthdayConfig>()?.Where(x => x.Enabled).ToList() ?? new List<BirthdayConfig>();
                var currentBirthdays = GetCurrentBirthdays();
                foreach (var guildConfig in guilds)
                {
                    if (!LocalManagementService.LastConfig.IsAcceptable(guildConfig.GuildId))
                    {
                        return;
                    }

                    var guild = Client.GetGuild(guildConfig.GuildId);
                    if (guild == null)
                    {
                        continue;
                    }

                    var role = guild.GetRole(guildConfig.BirthdayRole);
                    if (role == null)
                    {
                        return;
                    }

                    foreach (var user in role.Members)
                    {
                        if (currentBirthdays.All(x => x.UserId != user.Id))
                        {
                            await user.RemoveRoleAsync(role).ConfigureAwait(false);
                        }
                    }

                    var channel = guild.GetTextChannel(guildConfig.BirthdayAnnouncementChannelId);
                    if (channel == null)
                    {
                        continue;
                    }

                    await guild.DownloadUsersAsync().ContinueWith(async x =>
                    {
                        await NotifyGuild(currentBirthdays, guild, channel, role).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
            }
            finally
            {
                Running = false;
            }
        }

        public BirthdayConfig GetConfig(ulong guildId)
        {
            var config = Database.Load<BirthdayConfig>(BirthdayConfig.DocumentName(guildId));
            if (config == null)
            {
                config = new BirthdayConfig(guildId);
                Database.Store(config, BirthdayConfig.DocumentName(guildId));
            }

            return config;
        }

        public void SaveConfig(BirthdayConfig config)
        {
            Database.Store(config, BirthdayConfig.DocumentName(config.GuildId));
        }

        public BirthdayModel GetUser(ulong userId)
        {
            var config = Database.Load<BirthdayModel>(BirthdayModel.DocumentName(userId));
            return config;
        }

        public void SaveUser(BirthdayModel config)
        {
            Database.Store(config, BirthdayModel.DocumentName(config.UserId));
        }

        private async Task NotifyGuild(List<BirthdayModel> currentBirthdays, SocketGuild guild, SocketTextChannel channel, IRole role)
        {

            if (currentBirthdays.Any(x => guild.GetUser(x.UserId) != null))
            {
                var guildBirthdays = currentBirthdays.Select(x => (guild.GetUser(x.UserId), x)).Where(x => x.Item1 != null).ToList();

                foreach (var user in guildBirthdays)
                {
                    if (user.Item1.Roles.Any(x => x.Id == role.Id))
                    {
                        //Users are assigned the birthday role
                        //Therefore if they already have the role their birthday will have been announced.
                        continue;
                    }

                    await user.Item1.AddRoleAsync(role).ConfigureAwait(false);
                    await channel.SendMessageAsync($"{user.Item1.Mention}", false, new EmbedBuilder()
                    {
                        Title = $"Happy Birthday to {user.Item1.Nickname ?? user.Item1.Username}",
                            Description = user.Item2.ShowYear ? $"They are now: {user.Item2.Age()} years old" : null,
                            Color = Color.Blue,
                            Author = new EmbedAuthorBuilder()
                            {
                                IconUrl = user.Item1.GetAvatarUrl(),
                                    Name = user.Item1.Nickname ?? user.Item1.Username
                            }
                    }.Build()).ConfigureAwait(false);
                }
            }
        }

        private string[] DayTypes = { "d", "dd" };

        private string[] MonthTypes = { "MMM", "MMMM" };

        private string[] Delimeters = { " ", "-", "/" };

        public string[] GetTimeFormats(bool useYear)
        {
            var responses = new List<string>();
            foreach (var dayType in DayTypes)
            {
                var baseFormat = dayType;
                foreach (var monthType in MonthTypes)
                {
                    var secondaryFormat = $"{baseFormat} {monthType}";
                    if (useYear)
                    {
                        secondaryFormat += $" yyyy";
                    }

                    foreach (var delimiter in Delimeters)
                    {
                        var delimited = secondaryFormat.Replace(" ", delimiter);
                        responses.Add(delimited);
                    }
                }
            }

            return responses.ToArray();
        }
    }
}