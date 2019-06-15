using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.ELO.Methods;

namespace RavenBOT.Modules.ELO.Modules
{
    [RequireContext(ContextType.Guild)]
    [Group("elo")]
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        public Info(ELOService service)
        {
            Service = service;
        }

        public ELOService Service { get; }

        [Command("Register")]
        public async Task RegisterAsync(string name = null)
        {
            if (name == null)
            {
                name = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;
            }

            var player = Service.GetPlayer(Context.Guild.Id, Context.User.Id) ?? Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            var competition = Service.GetCompetition(Context.Guild.Id) ?? Service.CreateCompetition(Context.Guild.Id);
            await Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);
            await ReplyAsync("You have registered, all roles/name updates have been applied if applicable.");
        }
    }
}