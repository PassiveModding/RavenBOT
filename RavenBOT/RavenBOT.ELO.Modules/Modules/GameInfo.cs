using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.ELO.Modules.Modules
{
    public partial class Info
    {
        [Command("LastGame")]
        public async Task LastGameAsync(SocketGuildChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketGuildChannel;
            }
            //return the result of the last game if it can be found.
            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Specified channel is not a lobby.");
                return;
            }

            var game = Service.GetCurrentGame(lobby);
            if (game == null)
            {
                await ReplyAsync("Latest game is not available");
                return;
            }

            //TODO: Format and return info. perhaps just run the GameInfo command
        }

        [Command("GameInfo")]
        public async Task GameListAsync(int gameNumber, SocketGuildChannel lobbyChannel = null) //add functionality to specify lobby
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketGuildChannel;
            }

            //TODO: Perhaps make a separate method that just accepts the game info and formats it

            //return the result of the last game if it can be found.
            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Specified channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobbyChannel.Id, gameNumber);
            if (game == null)
            {
                await ReplyAsync("Latest game is not available");
                return;
            }

            //TODO: Format response
        }

        [Command("GameList")] //Showgames
        public async Task GameListAsync(SocketGuildChannel lobbyChannel = null)
        {
            //return a paginated message with history of all previous games for the specified (or current) lobby
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketGuildChannel;
            }

            //TODO: Check if necessary to load the lobby (what are the benefits of response vs performance hit of query)
            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Specified channel is not a lobby.");
                return;
            }

            var games = Service.GetGames(Context.Guild.Id, lobbyChannel.Id);

            //TODO: Paginate and format repsonse.
        }
    }
}