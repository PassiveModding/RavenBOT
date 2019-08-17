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
    [RavenRequireUserPermission(GuildPermission.Administrator)]
    public class CompetitionSetup : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public CompetitionSetup(ELOService service)
        {
            Service = service;
        }

        [Command("SetRegisterRole")]
        public async Task SetRegisterRole(IRole role)
        {
            var competition = Service.GetCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
            competition.RegisteredRankId = role.Id;
            Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Register role set.");
        }

        [Command("AddRank")]
        public async Task AddRank(IRole role, int points)
        {
            var competition = Service.GetCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != role.Id).ToList();
            competition.Ranks.Add(new Rank
            {
                RoleId = role.Id,
                    Points = points
            });
            Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Rank added.");
        }

        [Command("AddRank")]
        public async Task AddRank(int points, IRole role)
        {
            await AddRank(role, points);
        }

        [Command("RemoveRank")]
        public async Task RemoveRank(ulong roleId)
        {
            var competition = Service.GetCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != roleId).ToList();
            Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Rank Removed.");
        }

        [Command("RemoveRank")]
        public async Task RemoveRank(IRole role)
        {
            await RemoveRank(role.Id);
        }
    }
}