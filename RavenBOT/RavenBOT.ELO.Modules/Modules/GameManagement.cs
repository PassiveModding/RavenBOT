using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class GameManagement : ELOBase
    {
        //TODO: Ensure correct commands require mod/admin perms

        //GameResult (Allow players to vote on result), needs to be an optionla command, requires both team captains to vote
        //Game (Mods/admins submit game results), could potentially accept a comment for the result as well (ie for proof of wins)
        //UndoGame (would need to use the amount of points added to the user rather than calculate at command run time)

        [Command("Game")]
        public async Task GameAsync(int teamNumber, int gameNumber, SocketTextChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                //If no lobby is provided, assume that it is the current channel.
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Context.Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                return;
            }

            var game = Context.Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                //Reply not valid game number.
                return;
            }

            //TODO: Finish this.
            
        }
    }
}