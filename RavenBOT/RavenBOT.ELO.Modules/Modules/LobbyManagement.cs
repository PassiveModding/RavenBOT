using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Preconditions;

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
                var lobbyMatches = lobbies.Where(x => x.Queue.Contains(Context.User.Id));
                if (lobbyMatches.Any())
                {
                    var guildChannels = lobbyMatches.Select(x => Context.Guild.GetTextChannel(x.ChannelId)?.Mention ?? $"[{x.ChannelId}]");
                    await ReplyAsync($"MultiQueuing is not enabled in this server.\nPlease leave: {string.Join("\n", guildChannels)}");
                    return;
                }
            }

            //TODO: Check if game is picking players.
            //TODO: Create game on lobby full.
        }
    }
}