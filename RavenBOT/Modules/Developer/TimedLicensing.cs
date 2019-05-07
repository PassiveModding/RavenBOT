using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Developer
{
    //Example usage of timed license methods
    [RequireOwner]
    [Group("Developer Licensing Timed")]
    public class TimedLicensing : ModuleBase
    {
        public LicenseService LicenseService { get; }
        public string ServiceType = "ServiceName";

        public TimedLicensing(LicenseService licensing)
        {
            LicenseService = licensing;
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

            var user = LicenseService.GetTimedUser(ServiceType, Context.User.Id);
            var redeemResult = LicenseService.RedeemLicense(user, key);

            if (redeemResult == LicenseService.RedemptionResult.Success)
            {
                await ReplyAsync($"You have successfully redeemed the license. You can check your remaining time using the expiry command");
            }
            else if (redeemResult == LicenseService.RedemptionResult.AlreadyClaimed)
            {
                await ReplyAsync($"License Already Redeemed");
            }
            else if (redeemResult == LicenseService.RedemptionResult.InvalidKey)
            {
                await ReplyAsync("Invalid Key Provided");
            }
        }

        [Command("Expiry")]
        public async Task CheckBalance()
        {
            var user = LicenseService.GetTimedUser(ServiceType, Context.User.Id);
            await ReplyAsync($"Expires: {user.GetExpireTime().ToShortDateString()} {user.GetExpireTime().ToShortTimeString()}");
            if (user.GetExpireTime() > DateTime.UtcNow)
            {
                await ReplyAsync($"Remaining Time: {LicenseService.GetReadableLength(user.GetExpireTime() - DateTime.UtcNow)}");
            }
            else
            {
                await ReplyAsync($"Expired: {LicenseService.GetReadableLength(DateTime.UtcNow - user.GetExpireTime())} ago");
            }
        }

        [Command("HistoryTimed")]
        public async Task UserHistory()
        {
            var user = LicenseService.GetTimedUser(ServiceType, Context.User.Id);
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
            var newLicenses = LicenseService.MakeLicenses(ServiceType, quantity, time);
            await ReplyAsync($"{quantity} Licenses, {LicenseService.GetReadableLength(time)}\n" +
                             "```\n" +
                             $"{string.Join("\n", newLicenses.Select(x => x.Key))}\n" +
                             "```");
        }
    }
}
