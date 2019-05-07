using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Developer
{
    //Example usage of quantifiable license methods
    [RequireOwner]
    [Group("Developer.Licensing Quantifiable")]
    public class QuantifiableLicensing : ModuleBase
    {
        public LicenseService LicenseService { get; }
        public string ServiceType = "ServiceName";

        public QuantifiableLicensing(LicenseService licensing)
        {
            LicenseService = licensing;
        }

        [Command("Redeem")]
        public async Task RedeemUses([Remainder] string key = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                await ReplyAsync("Please enter a key or purchase one at: ...");
                return;
            }

            var user = LicenseService.GetQuantifiableUser(ServiceType, Context.User.Id);
            var redeemResult = LicenseService.RedeemLicense(user, key);

            if (redeemResult == LicenseService.RedemptionResult.Success)
            {
                await ReplyAsync($"You have successfully redeemed the license. Current Balance is: {user.RemainingUses()}");
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

        [Command("Balance")]
        public async Task CheckBalance()
        {
            var user = LicenseService.GetQuantifiableUser(ServiceType, Context.User.Id);
            await ReplyAsync($"User Balance is: {user.RemainingUses()}");
        }

        [Command("History")]
        public async Task UserHistory()
        {
            var user = LicenseService.GetQuantifiableUser(ServiceType, Context.User.Id);
            var history = "";
            foreach (var historyEntry in user.UserHistory)
            {
                history += $"[{historyEntry.Key.ToShortDateString()} {historyEntry.Key.ToShortTimeString()}] {historyEntry.Value}\n";
            }

            await ReplyAsync(history.FixLength());
        }

        [RequireOwner]
        [Command("GenerateLicenses")]
        public async Task GenerateLicenses(int quantity, int uses)
        {
            var newLicenses = LicenseService.MakeLicenses(ServiceType, quantity, uses);
            await ReplyAsync($"{quantity} Licenses, {uses} Uses\n" +
                             "```\n" +
                             $"{string.Join("\n", newLicenses.Select(x => x.Key))}\n" +
                             "```");
        }
    }
}
