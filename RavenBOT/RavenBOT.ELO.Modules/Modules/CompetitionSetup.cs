using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class CompetitionSetup : ELOBase
    {
        [Command("SetRegisterRole")]
        public async Task SetRegisterRole(IRole role)
        {
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.RegisteredRankId = role.Id;
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Register role set.");
        }

        [Command("AddRank")]
        public async Task AddRank(IRole role, int points)
        {
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != role.Id).ToList();
            competition.Ranks.Add(new Rank
            {
                RoleId = role.Id,
                    Points = points
            });
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
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
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != roleId).ToList();
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Rank Removed.");
        }

        [Command("RemoveRank")]
        public async Task RemoveRank(IRole role)
        {
            await RemoveRank(role.Id);
        }
    }
}