using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Info : ELOBase
    {
        [Command("Register")]
        public async Task RegisterAsync(string name = null)
        {
            if (name == null)
            {
                name = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;
            }

            var player = Context.CurrentPlayer ?? Context.Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            await Context.Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);
            await ReplyAsync("You have registered, all roles/name updates have been applied if applicable.");
        }
    }
}