using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class Info : ELOBase
    {
        [Command("Register")]
        public async Task RegisterAsync(string name = null)
        {
            if (name == null)
            {
                name = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;
            }

            //TODO: Add option to prevent re-registering
            //TODO: Add precondition for premium
            var player = Context.CurrentPlayer ?? Context.Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            await Context.Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);
            await ReplyAsync($"You have registered as {name}, all roles/name updates have been applied if applicable.");
        }

        [Command("Ranks")]
        public async Task ShowRanksAsync()
        {
            var comp = Context.Service.GetCompetition(Context.Guild.Id);
            if (!comp.Ranks.Any())
            {
                await ReplyAsync("There are currently no ranks set up.");
                return;
            }

            var msg = comp.Ranks.OrderByDescending(x => x.Points).Select(x =>  $"{Context.Guild.GetRole(x.RoleId)?.Mention ?? $"[{x.RoleId}]"} - {x.Points}").ToArray();
            await ReplyAsync("", false, string.Join("\n", msg).QuickEmbed());
        }
    }
}