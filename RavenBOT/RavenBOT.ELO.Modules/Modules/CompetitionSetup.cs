using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Methods.Migrations;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.ELO.Modules.Premium;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [Preconditions.RequireAdmin]
    public class CompetitionSetup : ReactiveBase
    {
        public ELOService Service { get; }
        public GuildService Prefix { get; }
        public PatreonIntegration PatreonIntegration { get; }
        public ELOMigrator Migrator { get; }

        public CompetitionSetup(ELOService service, GuildService prefix, PatreonIntegration patreonIntegration, ELOMigrator migrator)
        {
            this.Prefix = prefix;
            PatreonIntegration = patreonIntegration;
            Migrator = migrator;
            Service = service;
        }

        [Command("ClaimPremium", RunMode = RunMode.Sync)]
        public async Task ClaimPremiumAsync()
        {
            await PatreonIntegration.Claim(Context);
        }

        [Command("RedeemLegacyToken", RunMode = RunMode.Sync)]
        public async Task RedeemLegacyTokenAsync([Remainder]string token = null)
        {
            if (token == null)
            {
                await ReplyAsync("This is used to redeem tokens that were created using the old ELO version.");
                return;
            }

            if (Migrator.RedeemToken(Context.Guild.Id, token))
            {
                await ReplyAsync("Token redeemed.");
            }
            else
            {
                await ReplyAsync("Invalid token provided.");
            }
        }

        [Command("RegistrationLimit", RunMode = RunMode.Async)]
        public async Task GetRegisterLimit()
        {
            await ReplyAsync($"Current Limit is a maximum of: {PatreonIntegration.GetRegistrationLimit(Context)}");
        }

        [Command("CompetitionInfo", RunMode = RunMode.Async)]
        [Alias("CompetitionSettings", "GameSettings")]
        public async Task CompetitionInfo()
        {
            var comp = Service.GetOrCreateCompetition(Context.Guild.Id);
            var infoStr = $"**Register Role:** {MentionUtils.MentionRole(comp.RegisteredRankId)}\n" +
                        $"**Admin Role:** {comp.AdminRole}\n" +
                        $"**Moderator Role:** {MentionUtils.MentionRole(comp.ModeratorRole)}\n" +
                        $"**Update Nicknames:** {comp.UpdateNames}\n" +
                        $"**Nickname Format:** {comp.NameFormat}\n" +
                        $"**Block Multiqueuing:** {comp.BlockMultiQueueing}\n" +
                        $"**Allow Negative Score:** {comp.AllowNegativeScore}\n" +
                        $"**Default Loss Amount:** -{comp.DefaultLossModifier}\n" +
                        $"**Default Win Amount:** {comp.DefaultWinModifier}\n" +
                        $"**Allow Self Rename:** {comp.AllowSelfRename}\n" +
                        $"**Allow Re-registering:** {comp.AllowReRegister}\n" +
                        $"For rank info use the `ranks` command";
            await SimpleEmbedAsync(infoStr);
        }

        [Command("SetRegisterRole", RunMode = RunMode.Sync)]
        [Alias("Set RegisterRole", "RegisterRole")]
        [Summary("Sets or displays the current register role")]
        public async Task SetRegisterRole([Remainder] IRole role = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (role == null)
            {
                if (competition.RegisteredRankId != 0)
                {
                    var gRole = Context.Guild.GetRole(competition.RegisteredRankId);
                    if (gRole == null)
                    {
                        //Rank previously set but can no longer be found (deleted)
                        //May as well reset it.
                        competition.RegisteredRankId = 0;
                        Service.SaveCompetition(competition);
                        await ReplyAsync("Register role had previously been set but can no longer be found in the server. It has been reset.");
                    }
                    else
                    {
                        await ReplyAsync($"Current register role is: {gRole.Mention}");
                    }
                }
                else
                {
                    var serverPrefix = Prefix.GetPrefix(Context.Guild.Id) ?? Prefix.DefaultPrefix;
                    await ReplyAsync($"There is no register role set. You can set one with `{serverPrefix}SetRegisterRole @role` or `{serverPrefix}SetRegisterRole rolename`");
                }

                return;
            }

            competition.RegisteredRankId = role.Id;
            Service.SaveCompetition(competition);
            await ReplyAsync($"Register role set to {role.Mention}");
        }

        [Command("SetRegisterMessage", RunMode = RunMode.Sync)]
        [Alias("Set RegisterMessage", "RegisterMessage")]
        public async Task SetRegisterMessageAsync([Remainder] string message = null)
        {
            if (message == null)
            {
                message = "You have registered as `{name}`, all roles/name updates have been applied if applicable.";
            }
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.RegisterMessageTemplate = message;
            var testProfile = new Player(0, 0, "Player");
            testProfile.Wins = 5;
            testProfile.Losses = 2;
            testProfile.Draws = 1;
            testProfile.Points = 600;
            var exampleNick = competition.GetNickname(testProfile);

            Service.SaveCompetition(competition);
            await ReplyAsync($"Register Message set.\nExample:\n{exampleNick}");
        }

        [Command("RegisterMessageFormats", RunMode = RunMode.Async)]
        [Alias("RegisterFormats")]
        public async Task ShowRegistrationFormatsAsync()
        {
            var response = "**Register Message Formats**\n" + // Use Title
                "{score} - Total points\n" +
                "{name} - Registration name\n" +
                "{wins} - Total wins\n" +
                "{draws} - Total draws\n" +
                "{losses} - Total losses\n" +
                "{games} - Games played\n\n" +
                "Example:\n" +
                "`RegisterMessageFormats Thank you for registering {name}` `Thank you for registering Player`\n" +
                "NOTE: Format is limited to 1024 characters long";

            await SimpleEmbedAsync(response);
        }

        [Command("SetNicknameFormat", RunMode = RunMode.Sync)]
        [Alias("Set NicknameFormat", "NicknameFormat", "NameFormat", "SetNameFormat")]
        public async Task SetNicknameFormatAsync([Remainder] string format)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.NameFormat = format;
            var testProfile = new Player(0, 0, "Player");
            testProfile.Wins = 5;
            testProfile.Losses = 2;
            testProfile.Draws = 1;
            testProfile.Points = 600;
            var exampleNick = competition.GetNickname(testProfile);

            Service.SaveCompetition(competition);
            await ReplyAsync($"Nickname Format set.\nExample: `{exampleNick}`");
        }

        [Command("NicknameFormats", RunMode = RunMode.Async)]
        [Alias("NameFormats")]
        public async Task ShowNicknameFormatsAsync()
        {
            var response = "**NickNameFormats**\n" + // Use Title
                "{score} - Total points\n" +
                "{name} - Registration name\n" +
                "{wins} - Total wins\n" +
                "{draws} - Total draws\n" +
                "{losses} - Total losses\n" +
                "{games} - Games played\n\n" +
                "Examples:\n" +
                "`SetNicknameFormat {score} - {name}` `1000 - Player`\n" +
                "`SetNicknameFormat [{wins}] {name}` `[5] Player`\n" +
                "NOTE: Nicknames are limited to 32 characters long on discord";

            await ReplyAsync("", false, response.QuickEmbed());
        }

        [Command("AddRank", RunMode = RunMode.Sync)]
        [Alias("Add Rank", "UpdateRank")]
        public async Task AddRank(IRole role, int points)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != role.Id).ToList();
            competition.Ranks.Add(new Rank
            {
                RoleId = role.Id,
                    Points = points
            });
            Service.SaveCompetition(competition);
            await ReplyAsync("Rank added.");
        }

        [Command("AddRank", RunMode = RunMode.Sync)]
        [Alias("Add Rank", "UpdateRank")]
        public async Task AddRank(int points, IRole role)
        {
            await AddRank(role, points);
        }

        [Command("RemoveRank", RunMode = RunMode.Sync)]
        [Alias("Remove Rank", "DelRank")]
        public async Task RemoveRank(ulong roleId)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != roleId).ToList();
            Service.SaveCompetition(competition);
            await ReplyAsync("Rank Removed.");
        }

        [Command("RemoveRank", RunMode = RunMode.Sync)]
        [Alias("Remove Rank", "DelRank")]
        public async Task RemoveRank(IRole role)
        {
            await RemoveRank(role.Id);
        }

        [Command("AllowNegativeScore", RunMode = RunMode.Sync)]
        [Alias("AllowNegative")]
        public async Task AllowNegativeAsync(bool? allowNegative = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (allowNegative == null)
            {
                await ReplyAsync($"Allow Negative Score: {competition.AllowNegativeScore}");
                return;
            }
            competition.AllowNegativeScore = allowNegative.Value;
            Service.SaveCompetition(competition);
            await ReplyAsync($"Allow Negative Score: {allowNegative.Value}");
        }

        [Command("AllowReRegister", RunMode = RunMode.Sync)]
        public async Task AllowReRegisterAsync(bool? reRegister = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (reRegister == null)
            {
                await ReplyAsync($"Allow re-register: {competition.AllowReRegister}");
                return;
            }
            competition.AllowReRegister = reRegister.Value;
            Service.SaveCompetition(competition);
            await ReplyAsync($"Allow re-register: {reRegister.Value}");
        }
                
        [Command("AllowSelfRename", RunMode = RunMode.Sync)]
        public async Task AllowSelfRenameAsync(bool? selfRename = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (selfRename == null)
            {
                await ReplyAsync($"Allow Self Rename: {competition.AllowSelfRename}");
                return;
            }
            competition.AllowSelfRename = selfRename.Value;
            Service.SaveCompetition(competition);
            await ReplyAsync($"Allow Self Rename: {selfRename.Value}");
        }

        [Command("DefaultWinModifier", RunMode = RunMode.Sync)]
        public async Task CompWinModifier(int? amountToAdd = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);

            if (!amountToAdd.HasValue)
            {
                await ReplyAsync($"DefaultWinModifier: {competition.DefaultWinModifier}");
                return;
            }
            competition.DefaultWinModifier = amountToAdd.Value;
            Service.SaveCompetition(competition);
            await ReplyAsync("Competition Updated.");
        }

        
        [Command("DefaultLossModifier", RunMode = RunMode.Sync)]
        public async Task CompLossModifier(int? amountToSubtract = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            
            if (!amountToSubtract.HasValue)
            {
                await ReplyAsync($"DefaultLossModifier: {competition.DefaultLossModifier}");
                return;
            }
            competition.DefaultLossModifier = amountToSubtract.Value;
            Service.SaveCompetition(competition);
            await ReplyAsync("Competition Updated.");
        }

        [Command("RankLossModifier", RunMode = RunMode.Sync)]
        public async Task RankLossModifier(IRole role, int? amountToSubtract = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            var rank = competition.Ranks.FirstOrDefault(x => x.RoleId == role.Id);
            if (rank == null)
            {
                await ReplyAsync("Provided role is not a rank.");
                return;
            }

            rank.LossModifier = amountToSubtract;
            Service.SaveCompetition(competition);
            if (!amountToSubtract.HasValue)
            {
                await ReplyAsync($"This rank will now use the server's default loss value (-{competition.DefaultLossModifier}) when subtracting points.");
            }
            else
            {
                await ReplyAsync($"When a player with this rank loses they will lose {amountToSubtract} points");
            }
        }

        [Command("RankWinModifier", RunMode = RunMode.Sync)]
        public async Task RankWinModifier(IRole role, int? amountToAdd = null)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            var rank = competition.Ranks.FirstOrDefault(x => x.RoleId == role.Id);
            if (rank == null)
            {
                await ReplyAsync("Provided role is not a rank.");
                return;
            }

            rank.WinModifier = amountToAdd;
            Service.SaveCompetition(competition);
            if (!amountToAdd.HasValue)
            {
                await ReplyAsync($"This rank will now use the server's default win value (+{competition.DefaultWinModifier}) when adding points.");
            }
            else
            {
                await ReplyAsync($"When a player with this rank wins they will gain {amountToAdd} points");
            }
        }

        [Command("UpdateNicknames", RunMode = RunMode.Sync)]
        public async Task UpdateNicknames()
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.UpdateNames = !competition.UpdateNames;
            Service.SaveCompetition(competition);
            await ReplyAsync($"Update Nicknames: {competition.UpdateNames}");
        }

        
        [Command("CreateReactionRegistration", RunMode = RunMode.Sync)]
        public async Task CreateReactAsync([Remainder]string message = null)
        {
            var config = Service.GetReactiveRegistrationMessage(Context.Guild.Id);
            if (config == null)
            {
                config = new ELOService.ReactiveRegistrationMessage();
                config.GuildId = Context.Guild.Id;
            }

            var response = await SimpleEmbedAsync(message);
            config.MessageId = response.Id;
            Service.SaveReactiveRegistrationMessage(config);
            await response.AddReactionAsync(Service.registrationConfirmEmoji);
        }
    }
}