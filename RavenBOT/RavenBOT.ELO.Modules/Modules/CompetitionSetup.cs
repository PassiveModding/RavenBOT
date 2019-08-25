using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [Preconditions.RequireAdmin]
    public class CompetitionSetup : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public CompetitionSetup(ELOService service)
        {
            Service = service;
        }

        [Command("SetRegisterRole", RunMode = RunMode.Sync)]
        [Alias("Set RegisterRole")]
        public async Task SetRegisterRole(IRole role)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.RegisteredRankId = role.Id;
            Service.SaveCompetition(competition);
            await ReplyAsync("Register role set.");
        }

        [Command("SetNicknameFormat", RunMode = RunMode.Sync)]
        [Alias("Set NicknameFormat", "NameFormat", "SetNameFormat")]
        public async Task SetNicknameFormatAsync([Remainder]string format)
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
                           "`NicknameFormats {score} - {name}` `1000 - Player`\n" +
                           "`NicknameFormats [{wins}] {name}` `[5] Player`\n" +
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
