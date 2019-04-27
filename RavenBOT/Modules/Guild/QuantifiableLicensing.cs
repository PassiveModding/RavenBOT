using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Guild
{
    public class QuantifiableLicensing : ModuleBase
    {
        public QuantifiableLicenseService QuantifiableLicenseService { get; }

        public QuantifiableLicensing(QuantifiableLicenseService licensing)
        {
            QuantifiableLicenseService = licensing;
        }

        [Command("Redeem")]
        public async Task RedeemUses([Remainder] string key = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                //TODO: Add Store url
                await ReplyAsync("Please enter a key or purchase one at: ");
                return;
            }

            var user = QuantifiableLicenseService.GetUser(Context.User.Id);
            var redeemResult = QuantifiableLicenseService.RedeemLicense(user, key);

            if (redeemResult == QuantifiableLicenseService.RedemptionResult.Success)
            {
                await ReplyAsync($"You have successfully redeemed the license. Current Balance is: {user.RemainingUses()}");
            }
            else if (redeemResult == QuantifiableLicenseService.RedemptionResult.AlreadyClaimed)
            {
                await ReplyAsync($"License Already Redeemed");
            }
            else if (redeemResult == QuantifiableLicenseService.RedemptionResult.InvalidKey)
            {
                await ReplyAsync("Invalid Key Provided");
            }
        }

        [Command("Balance")]
        public async Task CheckBalance()
        {
            var user = QuantifiableLicenseService.GetUser(Context.User.Id);
            await ReplyAsync($"User Balance is: {user.RemainingUses()}");
        }

        [Command("History")]
        public async Task UserHistory()
        {
            var user = QuantifiableLicenseService.GetUser(Context.User.Id);
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
            var newLicenses = QuantifiableLicenseService.MakeLicenses(quantity, uses);
            await ReplyAsync($"{quantity} Licenses, {uses} Uses\n" +
                             "```\n" +
                             $"{string.Join("\n", newLicenses.Select(x => x.Key))}\n" +
                             "```");
        }
    }
}
