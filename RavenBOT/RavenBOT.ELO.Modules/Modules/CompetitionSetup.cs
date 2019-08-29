using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [Preconditions.RequireAdmin]
    public class CompetitionSetup : ReactiveBase
    {
        public ELOService Service { get; }
        public PrefixService Prefix { get; }

        public CompetitionSetup(ELOService service, PrefixService prefix)
        {
            this.Prefix = prefix;
            Service = service;
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

        [Command("NicknameFormats")]
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
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
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
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
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
    }
}