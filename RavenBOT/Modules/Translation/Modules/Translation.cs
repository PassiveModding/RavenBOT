using System;
using System.Collections.Generic;
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
    [Group("Translate")]
    public class Translation : InteractiveBase<ShardedCommandContext>
    {
        public TranslateService TranslateService { get; }
        public Translation(TranslateService translateService)
        {
            TranslateService = translateService;
        }

        [RequireContext(ContextType.Guild)]
        [Command("Translate", RunMode = RunMode.Async)]
        [Summary("Translate from one language to another")]
        public async Task Translate(LanguageMap.LanguageCode languageCode, [Remainder] string message)
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            //Ensure whitelist isn't enforced unless the list is populated
            if (config.WhitelistRoles.Any())
            {
                //Check to see if the user has a whitelisted role
                if (!config.WhitelistRoles.Any(x => (Context.User as IGuildUser)?.RoleIds.Contains(x) == true))
                {
                    await ReplyAsync("You do not have enough permissions to run translations.");
                    return;
                }
            }

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

        [Priority(100)]
        [Command("languages", RunMode = RunMode.Async)]
        [Summary("A list of available languages codes to convert between")]
        public async Task TranslateListAsync()
        {
            var embed2 = new EmbedBuilder();
            embed2.AddField("INFORMATION", "Format:\n" + "<Language> <Language Code>\n");
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

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Role Whitelist")]
        [Summary("Displays the role whitelist for translations")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ShowWhitelist()
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            if (!config.WhitelistRoles.Any())
            {
                await ReplyAsync("There are no whitelisted roles. ie. all users can translate messages.");
                return;
            }

            var roles = config.WhitelistRoles.Select(x => Context.Guild.GetRole(x)?.Mention ?? $"Deleted Role: [{x}]").ToList();

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Description = string.Join("\n", roles),
                    Title = "Role whitelist"
            }.Build());
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Whitelist Role")]
        [Summary("adds a role to the translation whitelist.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddWhitelistedRole(IRole role)
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            config.WhitelistRoles = config.WhitelistRoles.Where(x => x != role.Id).ToList();
            config.WhitelistRoles.Add(role.Id);
            TranslateService.SaveTranslateGuild(config);
            await ReplyAsync("Role has been whitelisted.");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Whitelist Remove Role")]
        [Summary("adds a role to the translation whitelist.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveWhitelistRole(IRole role)
        {
            await RemoveWhitelistRole(role.Id);
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Whitelist Remove Role")]
        [Summary("adds a role to the translation whitelist via ID.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveWhitelistRole(ulong roleId)
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            config.WhitelistRoles.Remove(roleId);
            TranslateService.SaveTranslateGuild(config);
            await ReplyAsync("Role removed.");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Toggle Reactions")]
        [Summary("Toggles Translate Reactions in the server")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ToggleReactions()
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            config.ReactionTranslations = !config.ReactionTranslations;
            TranslateService.SaveTranslateGuild(config);
            await ReplyAsync($"Translation Reactions Enabled: {config.ReactionTranslations}");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Toggle DM Translations")]
        [Summary("Toggles wether to direct message users translations rather then sending them to the channel")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ToggleDmReactions()
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            config.DirectMessageTranslations = !config.DirectMessageTranslations;
            TranslateService.SaveTranslateGuild(config);
            await ReplyAsync($"DM Users Translations: {config.DirectMessageTranslations}");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("RemoveEmote")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddPair(Emoji emote)
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);

            foreach (var pair in config.CustomPairs)
            {
                pair.EmoteMatches.Remove(emote.Name);
            }

            await ReplyAsync("Reaction removed.");
            TranslateService.SaveTranslateGuild(config);
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("AddPair")]
        [Summary("Adds a pair for reaction translations")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddPair(LanguageMap.LanguageCode code, Emoji emote)
        {
            await AddPair(emote, code);
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("List")]
        [Summary("List paired languages")]
        public Task ListAsync()
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            var fields = config.CustomPairs.Select(x => new EmbedFieldBuilder { Name = x.Language.ToString(), Value = string.Join("\n", x.EmoteMatches), IsInline = true }).ToList();
            var embed = new EmbedBuilder { Fields = fields };
            return ReplyAsync("", false, embed.Build());
        }

        [Priority(100)]
        [Command("Defaults")]
        [Summary("List Default paired languages")]
        public Task ListDefaultAsync()
        {
            var fields = LanguageMap.DefaultMap.OrderByDescending(x => x.EmoteMatches.Count).Select(x => new EmbedFieldBuilder { Name = x.Language.ToString(), Value = string.Join("\n", x.EmoteMatches), IsInline = true }).ToList();
            var embed = new EmbedBuilder { Fields = fields };
            return ReplyAsync("", false, embed.Build());
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("AddPair")]
        [Summary("Adds a pair for reaction translations")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddPair(Emoji emote, LanguageMap.LanguageCode code)
        {
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            var match = config.CustomPairs.FirstOrDefault(x => x.Language == code);

            if (match != null)
            {
                if (match.EmoteMatches.Any(x => x == emote.Name))
                {
                    await ReplyAsync("This emote is already configured to work with this language.");
                    return;
                }

                match.EmoteMatches.Add(emote.Name);
            }
            else
            {
                config.CustomPairs.Add(new LanguageMap.TranslationSet()
                {
                    EmoteMatches = new List<string> { emote.Name },
                        Language = code
                });
            }

            TranslateService.SaveTranslateGuild(config);
            await ReplyAsync($"{emote.Name} reactions will now translate messages to {code}");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Info")]
        [Summary("Translate Stats for the current guild")]
        public async Task LimitsAsync()
        {
            var license = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            await ReplyAsync($"Remaining characters: {license.RemainingUses()} \nTotal used: {license.TotalUsed}");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("Redeem")]
        [Summary("Redeems a translation license")]
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

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [Command("Remaining Characters")]
        [Summary("Shows the amount of remaining characters for translation")]
        public async Task CheckBalance()
        {
            var profile = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            await ReplyAsync($"Remaining characters: {profile.RemainingUses()}");
        }

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("History")]
        [Summary("Shows translation history")]
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

        [Priority(100)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("Settings")]
        [Summary("Shows settings")]
        public async Task Settings()
        {
            var profile = TranslateService.License.GetQuantifiableUser(TranslateService.TranslateType, Context.Guild.Id);
            var config = TranslateService.GetTranslateGuild(Context.Guild.Id);
            var roles = config.WhitelistRoles.Select(x => Context.Guild.GetRole(x)?.Mention ?? $"Deleted Role: [{x}]").ToList();

            await ReplyAsync($"Remaining Uses: {profile.RemainingUses()}\n" +
                $"Total Used: {profile.TotalUsed}\n" +
                $"DM Translations: {config.DirectMessageTranslations}\n" +
                $"Reaction Translations: {config.ReactionTranslations}\n" +
                $"Whitelisted Roles: {(roles.Any() ? string.Join("\n", roles) : "None")}\n" +
                $"Use the List and Defaults commands to see reactions settings.");
        }

        [Priority(100)]
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

        [Priority(100)]
        [RequireOwner]
        [Command("SetApiKey")]
        public async Task SetApiKey(string apiKey = null)
        {
            var config = TranslateService.GetTranslateConfig();
            config.APIKey = apiKey;
            await ReplyAsync("This will take effect after a restart.");
            TranslateService.SaveTranslateConfig(config);
        }

        [Priority(100)]
        [RequireOwner]
        [Command("ToggleTranslation")]
        [Summary("DEV: Toggles translation services for all services")]
        public async Task ToggleTranslation()
        {
            var config = TranslateService.GetTranslateConfig();
            config.Enabled = !config.Enabled;
            await ReplyAsync($"Translation Enabled: {config.Enabled}\nNOTE: API Key needs to be set in order for translations to run.");
            TranslateService.SaveTranslateConfig(config);
        }

        [Priority(100)]
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