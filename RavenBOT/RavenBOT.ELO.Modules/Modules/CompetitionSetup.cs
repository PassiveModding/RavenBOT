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
        public async Task SetRegisterRole(IRole role)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            competition.RegisteredRankId = role.Id;
            Service.SaveCompetition(competition);
            await ReplyAsync("Register role set.");
        }

        [Command("AddRank", RunMode = RunMode.Sync)]
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
        public async Task AddRank(int points, IRole role)
        {
            await AddRank(role, points);
        }

        [Command("RemoveRank", RunMode = RunMode.Sync)]
        public async Task RemoveRank(ulong roleId)
        {
            var competition = Service.GetOrCreateCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != roleId).ToList();
            Service.SaveCompetition(competition);
            await ReplyAsync("Rank Removed.");
        }

        [Command("RemoveRank", RunMode = RunMode.Sync)]
        public async Task RemoveRank(IRole role)
        {
            await RemoveRank(role.Id);
        }
    }
}