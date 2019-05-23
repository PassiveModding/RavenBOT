using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.Birthday.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Birthday.Modules
{
    [Group("Birthday.")]
    public class Birthday : InteractiveBase<ShardedCommandContext>
    {
        public BirthdayService BirthdayService {get;}
        public Birthday(DiscordShardedClient client, IDatabase database)
        {
            BirthdayService = new BirthdayService(client, database);
        }

        [Command("Toggle")]
        public async Task ToggleEnabled()
        {
            var model = BirthdayService.GetConfig(Context.Guild.Id);
            model.Enabled = !model.Enabled;
            BirthdayService.SaveConfig(model);

            await ReplyAsync($"Birthday Service Enabled: {model.Enabled}\n" +
                            $"NOTE: You need to run the setchannel and setrole command in order for this to work");
        }

        [Command("SetChannel")]
        public async Task SetChannel()
        {
            var model = BirthdayService.GetConfig(Context.Guild.Id);
            model.BirthdayAnnouncementChannelId = Context.Channel.Id;
            BirthdayService.SaveConfig(model);

            await ReplyAsync($"Birthday Service Enabled: {model.Enabled}\n" +
                            $"Channel has been set to: {Context.Channel.Name}");
        }

        [Command("SetRole")]
        [Alias("SetBirthdayRole")]
        public async Task SetBirthdayRole(IRole role = null)
        {
            var model = BirthdayService.GetConfig(Context.Guild.Id);
            model.BirthdayRole = role?.Id ?? 0;
            BirthdayService.SaveConfig(model);

            await ReplyAsync($"Birthday Service Enabled: {model.Enabled}\n" +
                            $"Birthday Role: {role?.Mention ?? "N/A"}");
        }

        /*
        [Command("SetTimeZone")]
        public async Task SetTimeZone([Remainder]string timezone)
        {
            var user = BirthdayService.GetUser(Context.User.Id);
            if (user == null)
            {
                await ReplyAsync("You must set your birthday prior to setting a time zone.");
                return;
            }

            try
            {
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                user.TimeZone = info;
                BirthdayService.SaveUser(user);
                await ReplyAsync("Time zone set.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await ReplyAsync("Invalid time zone, here is a list of valid ones.");
                await PagedReplyAsync(new PaginatedMessage
                {
                    Pages = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id).OrderBy(x => x).ToList().SplitList(20).Select(x => new PaginatedMessage.Page
                    {
                        Description = string.Join("\n", x).FixLength(1999)
                    }).ToList()
                }, new ReactionList
                {
                    Forward = true,
                    Backward = true,
                    First = true,
                    Last = true,
                    Jump = true,
                    Trash = true                    
                }, true);
            }
        }*/

        [Command("SetBirthday")]
        [Alias("Set Birthday")]
        public async Task SetBirthday([Remainder]string dateTime = null)
        {
            var user = BirthdayService.GetUser(Context.User.Id);
            if (user != null && user.Attempts >= 3)
            {
                await ReplyAsync("Your have already exhausted all 3 of your attempts to set your birthday.");
                return;
            }

            if (dateTime == null)
            {
                await ReplyAsync("Please use the following example to set your birthday: `01 Jan 2000` or `05 Feb`");
                return;
            }
            
            DateTime? parsedTime;
            if (DateTime.TryParseExact(dateTime, BirthdayService.GetTimeFormats(true), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime resultWithYear))
            {
                parsedTime = resultWithYear;
            } 
            else if (DateTime.TryParseExact(dateTime, BirthdayService.GetTimeFormats(false), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime resultWithoutYear))
            {
                parsedTime = new DateTime(0001, resultWithoutYear.Month, resultWithoutYear.Day);
            }
            else
            {
                await ReplyAsync("Unable to retrieve a valid date format. Please use the following example: `01 Jan 2000` or `05 Feb`");
                return;
            }

            if (parsedTime > DateTime.UtcNow)
            {
                await ReplyAsync("Birth date cannot be in the future");
                return;
            }

            if (user == null)
            {
                user = new Models.BirthdayModel(Context.User.Id, parsedTime.Value, parsedTime.Value.Year != 0001);
                user.Attempts = 1;
            }
            else
            {
                user.Birthday = parsedTime.Value;
                user.ShowYear = parsedTime.Value.Year != 0001;
                user.Attempts++;
            }

            BirthdayService.SaveUser(user);
            await ReplyAsync($"Birthday set to {parsedTime.Value.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(parsedTime.Value.Month)} {(parsedTime.Value.Year == 0001 ? "" : parsedTime.Value.Year.ToString())}");
        }
    }
}