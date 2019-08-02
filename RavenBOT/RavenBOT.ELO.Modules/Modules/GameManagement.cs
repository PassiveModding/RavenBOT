using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MoreLinq.Extensions;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;
using Discord;

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
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task GameAsync(int teamNumber, int gameNumber, SocketTextChannel lobbyChannel = null)
        {
            //TODO: Needs a way of cancelling games and calling draws
            if (teamNumber != 1 && teamNumber != 2)
            {
                await ReplyAsync("Team number must be either then number `1` or `2`");
                return;
            }

            if (lobbyChannel == null)
            {
                //If no lobby is provided, assume that it is the current channel.
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Context.Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            var game = Context.Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                //Reply not valid game number.
                await ReplyAsync($"Game number is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            //TODO: Finish this.
            var competition = Context.Service.GetCompetition(Context.Guild.Id);

            List<(Player, int, Rank, RankChangeState, Rank)> winList;
            List<(Player, int, Rank, RankChangeState, Rank)> loseList;
            if (teamNumber == 1)
            {
                winList = UpdateTeamScoresAsync(competition, true, game.Team1.Players);
                loseList = UpdateTeamScoresAsync(competition, false, game.Team2.Players);
            } else 
            {
                loseList = UpdateTeamScoresAsync(competition, false, game.Team1.Players);
                winList = UpdateTeamScoresAsync(competition, true, game.Team2.Players);
            }

            var allUsers = new List<(Player, int, Rank, RankChangeState, Rank)>();
            allUsers.AddRange(winList);
            allUsers.AddRange(loseList);
            
            foreach (var user in allUsers)
            {
                //Ignore user updates if they aren't found in the server.
                var gUser = Context.Guild.GetUser(user.Item1.UserId);
                if (gUser == null) continue;

                var displayName = $"[{user.Item1.Points}] - {user.Item1.DisplayName}";

                //TODO: Check if the user can have their nickname set.
                bool nickNameUpdate = false;
                if (gUser.Nickname != null)
                {
                    if (!gUser.Nickname.Equals(displayName))
                    {
                        nickNameUpdate = true;
                    }
                }

                //Remove the original role id
                var roleIds = gUser.Roles.Select(x => x.Id).ToList();

                //Add the new role id to the user roleids
                if (user.Item5 != null)
                {
                    roleIds.Add(user.Item5.RoleId);
                }

                //Check to see if the user's rank was changed and update accordingly
                //TODO: Check edge cases for when the user's rank is below the registered rank?
                //Potentially ensure that registered rank is not removed from user.
                //TODO: Look into if a user receives more points and skips a level what will happen.
                if (user.Item4 != RankChangeState.None)
                {
                    if (user.Item3 != null)
                    {
                        roleIds.Remove(user.Item3.RoleId);
                    }
                }
                
                bool updateRoles = false;
                //Compare the updated roles against the original roles for equality                
                if (!Enumerable.SequenceEqual(roleIds.Distinct().OrderBy(x => x), gUser.Roles.Select(x => x.Id).OrderBy(x => x)))
                {
                    updateRoles = true;
                }

                //TODO: Test if logic within modifyasync works as intended.
                if (updateRoles || nickNameUpdate)
                {
                    await gUser.ModifyAsync(x =>
                    {
                        if (nickNameUpdate)
                        {
                            x.Nickname = displayName;
                        }

                        if (updateRoles)
                        {
                            x.RoleIds = roleIds.ToArray();
                        }
                    });
                }
            }

            game.GameState = GameResult.State.Decided;
            game.UpdatedScores = allUsers.Select(x => (x.Item1.UserId, x.Item2)).ToList();
            game.WinningTeam = teamNumber;
            Context.Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));

            var winField = new EmbedFieldBuilder
            {
                Name = $"Winning Team, Team #{teamNumber}",
                Value = GetResponseContent(winList)
            };
            var loseField = new EmbedFieldBuilder
            {
                Name = $"Losing Team",
                Value = GetResponseContent(loseList)
            };
            var response = new EmbedBuilder
            {
                Fields = new List<EmbedFieldBuilder>{winField, loseField},
                Title = $"Game #{gameNumber} Result called by {Context.User.Username}#{Context.User.Discriminator}"
            };
            await ReplyAsync("", false, response.Build());
        }

        public string GetResponseContent(List<(Player, int, Rank, RankChangeState, Rank)> players)
        {
            return string.Join("\n", players.Select(x => 
                {
                    if (x.Item4 == RankChangeState.None) return $"[{x.Item1.Points}] {x.Item1.DisplayName} Points: {x.Item2}";
                    
                    SocketRole originalRole = null;
                    SocketRole newRole = null;
                    if (x.Item3 != null)
                    {
                        originalRole = Context.Guild.GetRole(x.Item3.RoleId);
                    }

                    if (x.Item5 != null)
                    {
                        newRole = Context.Guild.GetRole(x.Item5.RoleId);
                    }

                    return $"[{x.Item1.Points}] {x.Item1.DisplayName} Points: {x.Item2} Rank: {originalRole?.Mention ?? "N.A"} => {newRole?.Mention ?? "N/A"}";
                }));
        }

        public enum RankChangeState
        {
            Derank,
            RankUp,
            None
        }

        //returns a list of userIds and the amount of points they received/lost for the win/loss, and if the user lost/gained a rank
        //UserId, Points added/removed, rank before, rank modify state, rank after
        public List<(Player, int, Rank, RankChangeState, Rank)> UpdateTeamScoresAsync(CompetitionConfig competition, bool win, List<ulong> userIds)
        {
            var updates = new List<(Player, int, Rank, RankChangeState, Rank)>();
            foreach (var userId in userIds)
            {
                var botUser = Context.Service.GetPlayer(Context.Guild.Id, userId);
                if (botUser == null) continue;

                var maxRank = MaxRank(competition, botUser.Points);

                int updateVal;
                RankChangeState state = RankChangeState.None;
                Rank newRank = null;

                if (win)
                {
                    updateVal = maxRank?.WinModifier ?? competition.DefaultWinModifier;
                    botUser.Points += updateVal;
                    newRank = MaxRank(competition, botUser.Points);
                    if (newRank != null)
                    {
                        if (maxRank == null)
                        {
                            state = RankChangeState.RankUp;
                        } 
                        else if (newRank.RoleId != maxRank.RoleId)
                        {
                            state = RankChangeState.RankUp;
                        }                          
                    }
                }
                else
                {
                    updateVal = maxRank?.LossModifier ?? competition.DefaultLossModifier;
                    botUser.Points -= updateVal;
                    updateVal = -updateVal;
                    if (maxRank != null)
                    {
                        if (botUser.Points < maxRank.Points)
                        {
                            state = RankChangeState.Derank;
                            newRank = MaxRank(competition, botUser.Points);
                        }
                    }
                }

                updates.Add((botUser, updateVal, maxRank, state, newRank));

                //TODO: Rank checking.
                Context.Service.Database.Store(botUser, Player.DocumentName(botUser.GuildId, botUser.UserId));
            }

            return updates;
        }

        //Returns the highest rank that has less points that the provided amount
        public Rank MaxRank(CompetitionConfig comp, int points)
        {
            var maxRank = comp.Ranks.Where(x => x.Points <= points).OrderByDescending(x => x.Points).FirstOrDefault();
            if (maxRank == null)
            {
                return null;
            }

            return maxRank;
        }
    }
}