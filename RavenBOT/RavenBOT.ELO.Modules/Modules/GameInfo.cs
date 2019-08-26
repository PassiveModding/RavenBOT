using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    public partial class Info
    {
        [Command("LastGame")]
        [Alias("Last Game", "Latest Game", "LatestGame", "lg")]
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

            await DisplayGameAsync(game);
        }

        public async Task DisplayGameAsync(GameResult game)
        {
            var embed = new EmbedBuilder();
            var gameStateInfo = "";

            //This is needed to ensure that all members can be gotten in larger servers.
            await Context.Guild.DownloadUsersAsync();

            if (game.GameState == GameResult.State.Picking)
            {
                gameStateInfo = $"State: Picking Teams\n" +
                                $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                                $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}\n"+
                                $"Remaining Players:\n{await game.GetQueueRemainingPlayersString(Context.Guild)}\n";
            }
            else if (game.GameState == GameResult.State.Canceled)
            {
                if (Lobby.IsCaptains(game.GamePickMode))
                {
                    var remainingPlayers = game.GetQueueRemainingPlayers();
                    
                    if (remainingPlayers.Any())
                    {
                        gameStateInfo = $"State: Cancelled\n" +
                            $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                            $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}\n"+
                            $"Remaining Players:\n{string.Join("\n", await Context.Guild.GetUserMentionListAsync(remainingPlayers))}\n";
                    }
                    else
                    {
                        //TODO: Address repeat response below
                        gameStateInfo = $"State: Canceled\n" +
                            $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                            $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}";
                    }
                }
                else
                {
                    gameStateInfo = $"State: Canceled\n" +
                        $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                        $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}";
                }               
            }
            else if (game.GameState == GameResult.State.Draw)
            {
                gameStateInfo = $"Result: Draw\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}";
            }
            else if (game.GameState == GameResult.State.Undecided)
            {
                gameStateInfo = $"State: Undecided\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}";
            }
            else if (game.GameState == GameResult.State.Decided)
            {
                //TODO: Null check getwinning/losing team methods
                var pointsAwarded = new List<string>();
                var winners = game.GetWinningTeam();
                pointsAwarded.Add($"Team {winners.Item1}");

                foreach (var player in winners.Item2.Players)
                {
                    var eUser = Service.GetPlayer(Context.Guild.Id, player);
                    if (eUser == null) continue; 

                    var pointUpdate = game.UpdatedScores.FirstOrDefault(x => x.Item1 == player);
                    pointsAwarded.Add($"{eUser.GetDisplayName()} - +{pointUpdate.Item2}");
                }

                var losers = game.GetLosingTeam();
                pointsAwarded.Add($"Team {losers.Item1}");
                foreach (var player in losers.Item2.Players)
                {
                    var eUser = Service.GetPlayer(Context.Guild.Id, player);
                    if (eUser == null) continue; 

                    var pointUpdate = game.UpdatedScores.FirstOrDefault(x => x.Item1 == player);
                    pointsAwarded.Add($"{eUser.GetDisplayName()} - {pointUpdate.Item2}");
                }
                gameStateInfo = $"Result: Team {game.WinningTeam} Won\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(Context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(Context.Guild)}\n" +
                    //TODO: Paginate this and add to second page if message is too long.
                    $"Points Awarded:\n{string.Join("\n", pointsAwarded)}";
            }


            embed.Description = $"GameId: {game.GameId}\n" +
                                $"Lobby: {game.GetChannel(Context.Guild).Mention}\n" +
                                $"Creation Time: {game.CreationTime.ToShortDateString()} {game.CreationTime.ToShortTimeString()}\n" +
                                $"Pick Mode: {game.GamePickMode}\n" +
                                gameStateInfo.FixLength(2047);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("GameInfo")]
        [Alias("Game Info", "Show Game", "ShowGame", "sg")]
        public async Task GameListAsync(int gameNumber, SocketGuildChannel lobbyChannel = null) //add functionality to specify lobby
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketGuildChannel;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Specified channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobbyChannel.Id, gameNumber);
            if (game == null)
            {
                await ReplyAsync("Invalid Game Id");
                return;
            }

            await DisplayGameAsync(game);
        }

        [Command("GameList")]
        [Alias("Game List", "GamesList", "ShowGames", "ListGames")]
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

            var games = Service.GetGames(Context.Guild.Id, lobbyChannel.Id).OrderByDescending(x => x.GameId);
            if (games.Count() == 0)
            {
                await ReplyAsync("There aren't any games in history for the specified lobby.");
                return;
            }

            var gamePages = games.SplitList(20);
            var pages = new List<PaginatedMessage.Page>();
            foreach (var page in gamePages)
            {
                var content = page.Select(x => {
                    if (x.GameState == GameResult.State.Decided)
                    {
                        return $"#{x.GameId}: Team {x.WinningTeam}";
                    }
                    return $"#{x.GameId}: {x.GameState}";
                });
                pages.Add(new PaginatedMessage.Page
                {
                    Description = string.Join("\n", content).FixLength(1023)
                });
            }

            await PagedReplyAsync(new PaginatedMessage
            {
                Pages = pages
            }, new ReactionList
            {
                Forward = true,
                Backward = true,
                First = true,
                Last = true
            });
        }
    }
}