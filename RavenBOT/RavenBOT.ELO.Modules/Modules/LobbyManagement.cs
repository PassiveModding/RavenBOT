using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    [IsRegistered]
    public class LobbyManagement : ELOBase
    {
        [Command("Join")]
        [Alias("JoinLobby", "Join Lobby", "j")]
        public async Task JoinLobbyAsync()
        {
            var lobby = Context.Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("This channel is not a lobby.");
                return;
            }

            if (lobby.Queue.Count >= lobby.PlayersPerTeam * 2)
            {
                await ReplyAsync("Queue is full, wait for teams to be chosen before joining.");
                return;
            }

            if (Context.Service.GetCompetition(Context.Guild.Id).BlockMultiQueueing)
            {
                var lobbies = Context.Service.GetLobbies(Context.Guild.Id);
                if (lobbies.Any(x => x.Queue.Contains(Context.User.Id)))
                {
                    await ReplyAsync("Lobby c")
                }
            }
        }
    }
}