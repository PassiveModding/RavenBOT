using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Extensions;
using RavenBOT.Modules.Translation.Methods;
using RavenBOT.Modules.Translation.Models;
using RavenBOT.Services.Database;
using RavenBOT.Services.Licensing;

namespace RavenBOT.Modules.Translation.Modules
{
    [Group("Translation")]
    public class Translation : InteractiveBase<ShardedCommandContext>
    {
        public TranslateService TranslateService {get;}
        public Translation(TranslateService translateService)
        {
            TranslateService = translateService;
        }


        [RequireContext(ContextType.Guild)]
        [Command("Translate", RunMode = RunMode.Async)]
        [Summary("Translate from one language to another")]
        public async Task Translate(LanguageMap.LanguageCode languageCode, [Remainder] string message)
        {
            var response = TranslateService.Translate(Context.Guild.Id, message, languageCode);
            if (response.ResponseResult != TranslateService.TranslateResponse.Result.Success)
            {
                return;
            }
            var embed = TranslateService.GetTranslationEmbed(response);
            if (embed == null)
            {
                return;
            }
            await ReplyAsync("", false, embed.Build());
        }

        [Command("languages", RunMode = RunMode.Async)]
        [Summary("A list of available languages codes to convert between")]
        public async Task TranslateListAsync()
        {
            var embed2 = new EmbedBuilder();
            embed2.AddField("INFORMATION", "Format:\n" + "<Language> <Language Code>\n" + "Example Usage:\n" + "`.p translate <language code> <message>`\n" + "`.p translate es Hi there this will be converted to spanish`");
            embed2.AddField("A", "`af` - Afrikaans\n`sq` - Albanian\n`am` - Amharic\n`ar` - Arabic\n`hy` - Armenian\n`az` - Azeerbaijani\n");
            embed2.AddField("B", "`eu` - Basque\n`be` - Belarusian\n`bn` - Bengali\n`bs` - Bosnian\n`bg` - Bulgarian\n");
            embed2.AddField("C", "`ca` - Catalan\n`ceb` - Cebuano\n`zh_CN` - Chinese(Simplified)\n`zh_TW` - Chinese(Traditional)\n`co` - Corsican\n`hr` - Croatian\n`cs` - Czech\n");
            embed2.AddField("D", "`da` - Danish\n`nl` - Dutch\n");
            embed2.AddField("E", "`en` - English\n`eo` - Esperanto\n`et` - Estonian\n");
            embed2.AddField("F", "`fi` - Finnish\n`fr` - French\n`fy` - Frisian\n");
            embed2.AddField("G", "`gl` - Galician\n`ka` - Georgian\n`de` - German\n`el` - Greek\n`gu` - Gujarati\n");
            embed2.AddField("H", "`ht` - Haitian-Creole\n`ha` - Hausa\n`haw` - Hawaiian\n`iw` - Hebrew\n`hi` - Hindi\n`hmn` - Hmong\n`hu` - Hungarian\n");
            embed2.AddField("I", "`_is` - Icelandic \n`ig` - Igbo\n`id` - Indonesian\n`ga` - Irish\n`it` - Italian\n");
            embed2.AddField("J", "`ja` - Japanese\n`jw` - Javanese\n");
            embed2.AddField("K", "`kn` - Kannada\n`kk` - Kazakh\n`km` - Khmer\n`ko` - Korean\n`ku` - Kurdish\n`ky` - Kyrgyz\n");
            embed2.AddField("L", "`lo` - Lao\n`la` - Latin\n`lv` - Latvian\n`lt` - Lithuanian\n`lb` - Luxembourgish\n");
            embed2.AddField("M", "`mk` - Macedonian\n`mg` - Malagasy\n`ms` - Malay\n`ml` - Malayalam\n`mt` - Maltese\n`mi` - Maori\n`mr` - Marathi\n`mn` - Mongolian\n`my` - Myanmar(Burmese)\n");
            embed2.AddField("N", "`ne` - Nepali\n`no` - Norwegian\n`ny` - Nyanja(Chichewa)\n");
            embed2.AddField("P", "`ps` - Pashto\n`fa` - Persian\n`pl` - Polish\n`pt` - Portuguese\n`pa` - Punjabi\n");
            embed2.AddField("R", "`ro` - Romanian\n`ru` - Russian\n");
            embed2.AddField("S", "`sm` - Samoan\n`gd` - Scots-Gaelic\n`sr` - Serbian\n`st` - Sesotho\n`sn` - Shona\n`sd` - Sindhi\n`si` - Sinhala(Sinhalese)\n`sk` - Slovak\n`sl` - Slovenian\n`so` - Somali\n`es` - Spanish\n`su` - Sundanese\n`sw` - Swahili\n`sv` - Swedish\n");
            embed2.AddField("T", "`tl` - Tagalog(Filipino)\n`tg` - Tajik\n`ta` - Tamil\n`te` - Telugu\n`th` - Thai\n`tr` - Turkish\n");
            embed2.AddField("U", "`uk` - Ukrainian\n`ur` - Urdu\n`uz` - Uzbek\n");
            embed2.AddField("V", "`vi` - Vietnamese\n");
            embed2.AddField("W", "`cy` - Welsh\n\n");
            embed2.AddField("X", "`xh` - Xhosa\n");
            embed2.AddField("Y", "`yi` - Yiddish\n`yo` - Yoruba\n");
            embed2.AddField("Z", "`zu` - Zulu\n");
            await Context.User.SendMessageAsync("", false, embed2.Build());
            await ReplyAsync("DM Sent.");
        }

        [RequireContext(ContextType.Guild)]
        [Command("Info")]
        [Summary("Translate Stats for the current guild")]
        public async Task LimitsAsync()
        {
            var license = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            await ReplyAsync($"Remaining characters: {license.RemainingUses()} \nTotal used: {license.TotalUsed}");
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("Redeem")]
        public async Task RedeemUses([Remainder] string key = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                await ReplyAsync($"Please enter a key or purchase one at: {TranslateService.GetTranslateConfig().StoreUrl}");
                return;
            }

            var profile = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            var redeemResult = TranslateService.License.RedeemLicense(profile, key);

            if (redeemResult == LicenseService.RedemptionResult.Success)
            {
                await ReplyAsync($"You have successfully redeemed the license. Current Balance is: {profile.RemainingUses()}");
            }
            else if (redeemResult == LicenseService.RedemptionResult.AlreadyClaimed)
            {
                await ReplyAsync("License Already Redeemed");
            }
            else if (redeemResult == LicenseService.RedemptionResult.InvalidKey)
            {
                await ReplyAsync("Invalid Key Provided");
            }
        }

        [RequireContext(ContextType.Guild)]
        [Command("Remaining Characters")]
        public async Task CheckBalance()
        {
            var profile = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            await ReplyAsync($"Remaining characters: {profile.RemainingUses()}");
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("History")]
        public async Task ServerHistory()
        {
            var profile = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            var history = "";
            foreach (var historyEntry in profile.UserHistory)
            {
                history += $"[{historyEntry.Key.ToShortDateString()} {historyEntry.Key.ToShortTimeString()}] {historyEntry.Value}\n";
            }

            await ReplyAsync(history.FixLength());
        }

        [RequireOwner]
        [Command("GenerateLicenses")]
        public async Task GenerateLicenses(int quantity, int uses)
        {
            var newLicenses = TranslateService.License.MakeLicenses(TranslateService.TranslateType, quantity, uses);
            await ReplyAsync($"{quantity} Licenses, {uses} Uses\n" +
                             "```\n" +
                             $"{string.Join("\n", newLicenses.Select(x => x.Key))}\n" +
                             "```");
        }

        [RequireOwner]
        [Command("SetApiKey")]
        public async Task SetApiKey(string apiKey = null)
        {
            var config = TranslateService.GetTranslateConfig();
            config.APIKey = apiKey;
            await ReplyAsync("This will take effect after a restart.");
            TranslateService.SaveTranslateConfig(config);
        }

        [RequireOwner]
        [Command("ToggleTranslation")]
        public async Task ToggleTranslation()
        {
            var config = TranslateService.GetTranslateConfig();
            config.Enabled = !config.Enabled;
            await ReplyAsync($"Translation Enabled: {config.Enabled}\nNOTE: API Key needs to be set in order for translations to run.");
            TranslateService.SaveTranslateConfig(config);
        }

        [RequireOwner]
        [Command("SetStoreUrl")]
        public async Task SetStoreUrl(string storeUrl = null)
        {
            var config = TranslateService.GetTranslateConfig();
            config.StoreUrl = storeUrl;
            await ReplyAsync("Store url set.");
            TranslateService.SaveTranslateConfig(config);
        }
    }
}