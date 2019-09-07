using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Methods
{
    public partial class ELOService
    {
        public enum GameFlag
        {
            gamestate,
            map,
            time,
            lobby,
            pickmode,
            usermentions,
            submitter
        }
        public async Task<(string, EmbedBuilder)> GetGameMessageAsync(SocketCommandContext context, GameResult game, string title = null, params GameFlag[] flags)
        {
            bool usermentions = flags.Contains(GameFlag.usermentions);

            bool gamestate = flags.Contains(GameFlag.gamestate);
            bool map = flags.Contains(GameFlag.map);
            bool time = flags.Contains(GameFlag.time);
            bool lobby = flags.Contains(GameFlag.lobby);
            bool pickmode = flags.Contains(GameFlag.pickmode);
            bool submitter = flags.Contains(GameFlag.submitter);
            bool remainingPlayers = false;
            bool winningteam = false;
            bool team1 = false;
            bool team2 = false;

            var message = usermentions ? string.Join(" ", game.Queue.Select(x => MentionUtils.MentionUser(x))) : "";
            
            var embed = new EmbedBuilder();
            embed.Title = title ?? $"Game #{game.GameId}";
            var desc = "";

            if (time)
            {
                desc += $"**Creation Time:** {game.CreationTime.ToShortDateString()} {game.CreationTime.ToShortTimeString()}\n";
            }

            if (pickmode)
            {
                desc += $"**Pick Mode:** {game.GamePickMode}\n";
            }

            if (lobby)
            {
                desc += $"**Lobby:** {MentionUtils.MentionChannel(game.LobbyId)}\n";
            }

            if (map && game.MapName != null)
            {
                desc += $"**Map:** {game.MapName}\n";
            }

            if (gamestate)
            {
                team1 = true;
                team2 = true;

                switch (game.GameState)
                {
                    case GameResult.State.Canceled:
                        desc += "**State:** Cancelled\n";
                        break;
                    case GameResult.State.Draw:
                        desc += "**State:** Draw\n";
                        break;
                    case GameResult.State.Picking:
                        remainingPlayers = true;
                        break;
                    case GameResult.State.Decided:
                        winningteam = true;
                        break;
                    case GameResult.State.Undecided:
                        break;
                }
            }

            if (winningteam)
            {
                var teamInfo = game.GetWinningTeam();
                embed.AddField($"Winning Team, Team #{teamInfo.Item1}", await teamInfo.Item2.GetTeamInfo(context.Guild));
                if (teamInfo.Item1 == 1)
                {
                    team1 = false;
                }
                else if (teamInfo.Item1 == 2)
                {
                    team2 = false;
                }
            }

            if (team1)
            {
                embed.AddField("Team 1", await game.Team1.GetTeamInfo(context.Guild));
            }

            if (team2)
            {
                embed.AddField("Team 2", await game.Team2.GetTeamInfo(context.Guild));
            }

            if (remainingPlayers)
            {
                embed.AddField("Remaining Players", string.Join(" ", game.GetQueueRemainingPlayers()));
            }

            embed.Description = desc;



            return (message, embed);
        }


        public async Task<EmbedBuilder> GetGameEmbedAsync(SocketCommandContext context, GameResult game)
        {
            var embed = new EmbedBuilder();
            var gameStateInfo = "";

            //This is needed to ensure that all members can be gotten in larger servers.
            await context.Guild.DownloadUsersAsync();

            if (game.GameState == GameResult.State.Picking)
            {
                gameStateInfo = $"State: Picking Teams\n" +
                                $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                                $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}\n"+
                                $"Remaining Players:\n{await game.GetQueueRemainingPlayersString(context.Guild)}\n";
            }
            else if (game.GameState == GameResult.State.Canceled)
            {
                if (Lobby.IsCaptains(game.GamePickMode))
                {
                    var remainingPlayers = game.GetQueueRemainingPlayers();
                    
                    if (remainingPlayers.Any())
                    {
                        gameStateInfo = $"State: Cancelled\n" +
                            $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                            $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}\n"+
                            $"Remaining Players:\n{string.Join("\n", await context.Guild.GetUserMentionListAsync(remainingPlayers))}\n";
                    }
                    else
                    {
                        //TODO: Address repeat response below
                        gameStateInfo = $"State: Canceled\n" +
                            $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                            $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}";
                    }
                }
                else
                {
                    gameStateInfo = $"State: Canceled\n" +
                        $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                        $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}";
                }               
            }
            else if (game.GameState == GameResult.State.Draw)
            {
                gameStateInfo = $"Result: Draw\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}";
            }
            else if (game.GameState == GameResult.State.Undecided)
            {
                gameStateInfo = $"State: Undecided\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}";
            }
            else if (game.GameState == GameResult.State.Decided)
            {
                //TODO: Null check getwinning/losing team methods
                var pointsAwarded = new List<string>();
                var winners = game.GetWinningTeam();
                pointsAwarded.Add($"Team {winners.Item1}");

                foreach (var player in winners.Item2.Players)
                {
                    var eUser = GetPlayer(context.Guild.Id, player);
                    if (eUser == null) continue; 

                    var pointUpdate = game.UpdatedScores.FirstOrDefault(x => x.Item1 == player);
                    pointsAwarded.Add($"{eUser.DisplayName} - +{pointUpdate.Item2}");
                }

                var losers = game.GetLosingTeam();
                pointsAwarded.Add($"Team {losers.Item1}");
                foreach (var player in losers.Item2.Players)
                {
                    var eUser = GetPlayer(context.Guild.Id, player);
                    if (eUser == null) continue; 

                    var pointUpdate = game.UpdatedScores.FirstOrDefault(x => x.Item1 == player);
                    pointsAwarded.Add($"{eUser.DisplayName} - {pointUpdate.Item2}");
                }
                gameStateInfo = $"Result: Team {game.WinningTeam} Won\n" +
                    $"Team 1:\n{await game.Team1.GetTeamInfo(context.Guild)}\n"+
                    $"Team 2:\n{await game.Team2.GetTeamInfo(context.Guild)}\n" +
                    //TODO: Paginate this and add to second page if message is too long.
                    $"Points Awarded:\n{string.Join("\n", pointsAwarded)}";
            }


            embed.Description = $"GameId: {game.GameId}\n" +
                                $"Lobby: {game.GetChannel(context.Guild).Mention}\n" +
                                $"Creation Time: {game.CreationTime.ToShortDateString()} {game.CreationTime.ToShortTimeString()}\n" +
                                $"Pick Mode: {game.GamePickMode}\n" +
                                $"{(game.MapName == null ? "" : $"Map: {game.MapName}\n")}" +
                                gameStateInfo.FixLength(2047);

            return embed;
        }
    }
}