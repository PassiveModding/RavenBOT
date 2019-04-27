using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Guild
{
    public class TimedLicensing : ModuleBase
    {
        public TimedLicenseService TimedLicenseService { get; }

        public TimedLicensing(TimedLicenseService licensing)
        {
            TimedLicenseService = licensing;
        }

        [Command("RedeemTimed")]
        public async Task RedeemUses([Remainder] string key = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                //TODO: Add Store url
                await ReplyAsync("Please enter a key or purchase one at: ");
                return;
            }

            var user = TimedLicenseService.GetUser(Context.User.Id);
            var redeemResult = TimedLicenseService.RedeemLicense(user, key);

            if (redeemResult == TimedLicenseService.RedemptionResult.Success)
            {
                await ReplyAsync($"You have successfully redeemed the license. You can check your remaining time using the expiry command");
            }
            else if (redeemResult == TimedLicenseService.RedemptionResult.AlreadyClaimed)
            {
                await ReplyAsync($"License Already Redeemed");
            }
            else if (redeemResult == TimedLicenseService.RedemptionResult.InvalidKey)
            {
                await ReplyAsync("Invalid Key Provided");
            }
        }

        [Command("Expiry")]
        public async Task CheckBalance()
        {
            var user = TimedLicenseService.GetUser(Context.User.Id);
            await ReplyAsync($"Expires: {user.GetExpireTime().ToShortDateString()} {user.GetExpireTime().ToShortTimeString()}");
            if (user.GetExpireTime() > DateTime.UtcNow)
            {
                await ReplyAsync($"Remaining Time: {TimedLicenseService.GetReadableLength(user.GetExpireTime() - DateTime.UtcNow)}");
            }
            else
            {
                await ReplyAsync($"Expired: {TimedLicenseService.GetReadableLength(DateTime.UtcNow - user.GetExpireTime())} ago");
            }
        }

        [Command("HistoryTimed")]
        public async Task UserHistory()
        {
            var user = TimedLicenseService.GetUser(Context.User.Id);
            var history = "";
            foreach (var historyEntry in user.UserHistory)
            {
                history += $"[{historyEntry.Key.ToShortDateString()} {historyEntry.Key.ToShortTimeString()}] {historyEntry.Value}\n";
            }

            await ReplyAsync(history.FixLength());
        }

        [RequireOwner]
        [Command("GenerateTimedLicenses")]
        public async Task GenerateLicenses(int quantity, TimeSpan time)
        {
            var newLicenses = TimedLicenseService.MakeLicenses(quantity, time);
            await ReplyAsync($"{quantity} Licenses, {TimedLicenseService.GetReadableLength(time)}\n" +
                             "```\n" +
                             $"{string.Join("\n", newLicenses.Select(x => x.Key))}\n" +
                             "```");
        }
    }
}
