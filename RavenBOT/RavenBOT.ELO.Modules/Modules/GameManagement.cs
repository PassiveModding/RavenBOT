using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class GameManagement : ReactiveBase
    {
        public ELOService Service { get; }

        public GameManagement(ELOService service)
        {
            Service = service;
        }

        //TODO: Ensure correct commands require mod/admin perms

        //GameResult (Allow players to vote on result), needs to be an optional command, requires both team captains to vote
        //Game (Mods/admins submit game results), could potentially accept a comment for the result as well (ie for proof of wins)
        //UndoGame (would need to use the amount of points added to the user rather than calculate at command run time)

        [Command("Results")]
        public async Task ShowResultsAsync()
        {
            await ReplyAsync(string.Join("\n", Extensions.EnumNames<GameResult.Vote.VoteState>()));
        }        


        [Command("Result", RunMode = RunMode.Sync)]
        public async Task GameResultAsync(SocketTextChannel lobbyChannel, int gameNumber, string voteState)
        {
            await GameResultAsync(gameNumber, voteState, lobbyChannel);
        }

        [Command("Result", RunMode = RunMode.Sync)]
        public async Task GameResultAsync(int gameNumber, string voteState, SocketTextChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            //Do vote conversion to ensure that the state is a string and not an int (to avoid confusion with team number from old elo version)
            if (int.TryParse(voteState, out var voteNumber))
            {
                await ReplyAsync("Please supply a result relevant to you rather than the team number. Use the `Results` command to see a list of these.");
                return;
            }
            if (!Enum.TryParse(voteState, true, out GameResult.Vote.VoteState vote))
            {
                await ReplyAsync("Your vote was invalid. Please choose a result relevant to you. ie. Win (if you won the game) or Lose (if you lost the game)\nYou can view all possible results using the `Results` command.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobbyChannel.Id, gameNumber);
            if (game == null)
            {
                await ReplyAsync("GameID is invalid.");
                return;
            }

            if (game.GameState != GameResult.State.Undecided)
            {
                await ReplyAsync("You can only vote on the result of undecided games.");
                return;
            }
            else if (game.VoteComplete)
            {
                //Result is undecided but vote has taken place, therefore it wasn't unanimous
                await ReplyAsync("Vote has already been taken on this game but wasn't unanimous, ask an admin to submit the result");
                return;
            }

            if (!game.Team1.Players.Contains(Context.User.Id) && !game.Team2.Players.Contains(Context.User.Id))
            {
                await ReplyAsync("You are not a player in this game and cannot vote on it's result.");
                return;
            }

            if (game.Votes.ContainsKey(Context.User.Id))
            {
                await ReplyAsync("You already submitted your vote for this game.");
                return;
            }

            var userVote = new GameResult.Vote()
            {
                UserId = Context.User.Id,
                UserVote = vote
            };

            game.Votes.Add(Context.User.Id, userVote);
            
            //Ensure votes is greater than half the amount of players.
            if (game.Votes.Count * 2 > game.Team1.Players.Count + game.Team2.Players.Count)
            {
                var drawCount = game.Votes.Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Draw);
                var cancelCount = game.Votes.Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Cancel);

                var team1WinCount = game.Votes
                                        //Get players in team 1 and count wins
                                        .Where(x => game.Team1.Players.Contains(x.Key))
                                        .Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Win)
                                    +
                                    game.Votes
                                        //Get players in team 2 and count losses
                                        .Where(x => game.Team2.Players.Contains(x.Key))
                                        .Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Lose);

                var team2WinCount = game.Votes
                                        //Get players in team 2 and count wins
                                        .Where(x => game.Team2.Players.Contains(x.Key))
                                        .Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Win)
                                    +
                                    game.Votes
                                        //Get players in team 1 and count losses
                                        .Where(x => game.Team1.Players.Contains(x.Key))
                                        .Count(x => x.Value.UserVote == GameResult.Vote.VoteState.Lose);

                if (team1WinCount == game.Votes.Count)
                {
                    //team1 win
                    Service.SaveGame(game);
                    await GameAsync(gameNumber, 1, lobbyChannel, "Decided by vote.");
                }
                else if (team2WinCount == game.Votes.Count)
                {
                    //team2 win
                    Service.SaveGame(game);
                    await GameAsync(gameNumber, 2, lobbyChannel, "Decided by vote.");
                }
                else if (drawCount == game.Votes.Count)
                {
                    //draw
                    Service.SaveGame(game);
                    await DrawAsync(gameNumber, lobbyChannel, "Decided by vote.");
                }
                else if (cancelCount == game.Votes.Count)
                {
                    //cancel
                    Service.SaveGame(game);
                    await CancelAsync(gameNumber, lobbyChannel, "Decided by vote.");
                }
                else
                {
                    //Lock game votes and require admin to decide.
                    //TODO: Show votes by whoever
                    await ReplyAsync("Vote was not unanimous, game result must be decided by a moderator.");
                    game.VoteComplete = true;
                    Service.SaveGame(game);
                    return;
                }
            }
            else
            {
                Service.SaveGame(game);
                await ReplyAsync($"Vote counted as: {vote.ToString()}");
            }            
        }

        [Command("UndoGame", RunMode = RunMode.Sync)]
        [Alias("Undo Game")]
        [Preconditions.RequireModerator]
        public async Task UndoGameAsync(SocketTextChannel lobbyChannel , int gameNumber)
        {
            await UndoGameAsync(gameNumber, lobbyChannel);
        }

        [Command("UndoGame", RunMode = RunMode.Sync)]
        [Alias("Undo Game")]
        [Preconditions.RequireModerator]
        public async Task UndoGameAsync(int gameNumber, SocketTextChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);
            if (competition == null)
            {
                await ReplyAsync("Not a competition.");
                return;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                await ReplyAsync($"GameID is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            if (game.GameState != GameResult.State.Decided)
            {
                await ReplyAsync("Game result is not decided. NOTE: Draw results cannot currently be undone.");
                return;
            }

            await UpdateScores(lobby, game, competition);
            //TODO: Announce the undone game
        }

        public async Task AnnounceResultAsync(Lobby lobby, EmbedBuilder builder)
        {
            if (lobby.GameResultAnnouncementChannel != 0 && lobby.GameResultAnnouncementChannel != Context.Channel.Id)
            {
                var channel = Context.Guild.GetTextChannel(lobby.GameResultAnnouncementChannel);
                if (channel != null)
                {
                    try
                    {
                        await channel.SendMessageAsync("", false, builder.Build());
                    }
                    catch
                    {
                        //
                    }
                }
            }

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        public async Task AnnounceResultAsync(Lobby lobby, GameResult game)
        {
            var embed = Service.GetGameEmbed(Context, game);
            await AnnounceResultAsync(lobby, embed);
        }


        public async Task UpdateScores(Lobby lobby, GameResult game, CompetitionConfig competition)
        {
            foreach (var score in game.ScoreUpdates)
            {
                var player = Service.GetPlayer(Context.Guild.Id, score.Key);
                if (player == null)
                {
                    //Skip if for whatever reason the player profile cannot be found.
                    continue;
                }

                var currentRank = competition.MaxRank(player.Points);

                if (score.Value < 0)
                {
                    //Points lost, so add them back
                    player.Losses--;
                    player.SetPoints(competition, player.Points + Math.Abs(score.Value));
                }
                else
                {
                    //Points gained so remove them
                    player.Wins--;
                    player.SetPoints(competition, player.Points - Math.Abs(score.Value));
                }

                //Save the player profile after updating scores.
                Service.SavePlayer(player);

                var guildUser = Context.Guild.GetUser(player.UserId);
                if (guildUser == null)
                {
                    //The user cannot be found in the server so skip updating their name/profile
                    continue;
                }
                
                var displayName = competition.GetNickname(player);
                bool nicknameChange = false;
                if (guildUser.Nickname != null && competition.UpdateNames) 
                {
                    if (!guildUser.Nickname.Equals(displayName))
                    {
                        nicknameChange = true;
                    }
                }

                //TODO: Rank updates
                bool rankChange = false;
                var newRank = competition.MaxRank(player.Points);
                var currentRoles = guildUser.Roles.Select(x => x.Id).ToList();
                if (currentRank == null)
                {
                    if (newRank != null)
                    {
                        //Add the new rank.
                        currentRoles.Add(newRank.RoleId);
                        rankChange = true;
                    }
                }
                else if (newRank != null)
                {
                    //Current rank and new rank are both not null
                    if (currentRank.RoleId != newRank.RoleId)
                    {
                        currentRoles.Remove(currentRank.RoleId);
                        currentRoles.Add(newRank.RoleId);
                        rankChange = true;
                    }
                }
                else
                {
                    //Current rank exists but new rank is null
                    //Remove the current rank.
                    currentRoles.Remove(currentRank.RoleId);
                    rankChange = true;
                }

                if (rankChange || nicknameChange)
                {
                    try
                    {
                        await guildUser.ModifyAsync(x =>
                        {
                            if (nicknameChange)
                            {
                                x.Nickname = displayName;
                            }

                            if (rankChange)
                            {
                                //Set the user's roles to the modified list which removes and lost ranks and adds any gained ranks
                                x.RoleIds = currentRoles.Where(r => r != Context.Guild.EveryoneRole.Id).ToArray();
                            }
                        });
                    }
                    catch
                    {
                        //TODO: Add to list of name change errors.
                    }
                }
            }

            game.GameState = GameResult.State.Undecided;
            game.ScoreUpdates = new Dictionary <ulong, int> ();
            Service.SaveGame(game);
        }

        [Command("DeleteGame", RunMode = RunMode.Sync)]
        [Alias("Delete Game", "DelGame")]
        [Preconditions.RequireAdmin]
        //TODO: Explain that this does not affect the users who were in the game if it had a result. this is only for removing the game log from the database
        public async Task DelGame(SocketTextChannel lobbyChannel, int gameNumber)
        {
            await DelGame(gameNumber, lobbyChannel);
        }

        [Command("DeleteGame", RunMode = RunMode.Sync)]
        [Alias("Delete Game", "DelGame")]
        [Preconditions.RequireAdmin]
        //TODO: Explain that this does not affect the users who were in the game if it had a result. this is only for removing the game log from the database
        public async Task DelGame(int gameNumber, SocketTextChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobbyChannel.Id, gameNumber);
            if (game == null)
            {
                await ReplyAsync("Invalid GameID.");
                return;
            }

            Service.RemoveGame(game);
            await ReplyAsync("Game Deleted.", false, JsonConvert.SerializeObject(game, Formatting.Indented).FixLength(2047).QuickEmbed());
        }


        [Command("Cancel", RunMode = RunMode.Sync)]
        [Alias("CancelGame")]
        [Preconditions.RequireModerator]
        public async Task CancelAsync(SocketTextChannel lobbyChannel, int gameNumber, [Remainder]string comment = null)
        {
            await CancelAsync(gameNumber, lobbyChannel, comment);
        }

        [Command("Cancel", RunMode = RunMode.Sync)]
        [Alias("CancelGame")]
        [Preconditions.RequireModerator]
        public async Task CancelAsync(int gameNumber, SocketTextChannel lobbyChannel = null, [Remainder]string comment = null)
        {
            if (lobbyChannel == null)
            {
                //If no lobby is provided, assume that it is the current channel.
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                //Reply not valid game number.
                await ReplyAsync($"GameID is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            game.GameState = GameResult.State.Canceled;
            game.Submitter = Context.User.Id;
            game.Comment = comment;
            game.ScoreUpdates = new Dictionary<ulong, int>();
            Service.SaveGame(game);

            await AnnounceResultAsync(lobby, game);
        }


        [Command("Draw", RunMode = RunMode.Sync)]
        [Preconditions.RequireModerator]
        public async Task DrawAsync(SocketTextChannel lobbyChannel, int gameNumber, [Remainder]string comment = null)
        {
            await DrawAsync(gameNumber, lobbyChannel, comment);
        }

        [Command("Draw", RunMode = RunMode.Sync)]
        [Preconditions.RequireModerator]
        public async Task DrawAsync(int gameNumber, SocketTextChannel lobbyChannel = null, [Remainder]string comment = null)
        {
            if (lobbyChannel == null)
            {
                //If no lobby is provided, assume that it is the current channel.
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                //Reply not valid game number.
                await ReplyAsync($"GameID is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            game.GameState = GameResult.State.Draw;
            game.Submitter = Context.User.Id;
            game.Comment = comment;
            game.ScoreUpdates = new Dictionary<ulong, int>();
            Service.SaveGame(game);

            await DrawPlayersAsync(game.Team1.Players);
            await DrawPlayersAsync(game.Team2.Players);
            await ReplyAsync($"Called draw on game #{game.GameId}, player's game and draw counts have been updated.");
            await AnnounceResultAsync(lobby, game);
        }

        public Task DrawPlayersAsync(HashSet<ulong> playerIds)
        {
            foreach (var id in playerIds)
            {
                var player = Service.GetPlayer(Context.Guild.Id, id);
                if (player == null) continue;

                player.Draws++;
                Service.SavePlayer(player);
            }

            return Task.CompletedTask;
        }

        
        [Command("Game", RunMode = RunMode.Sync)]
        [Alias("g")]
        [Preconditions.RequireModerator]
        public async Task GameAsync(SocketTextChannel lobbyChannel, int gameNumber, int winningTeamNumber, [Remainder]string comment = null)
        {
            await GameAsync(winningTeamNumber, gameNumber, lobbyChannel, comment);
        }

        [Command("Game", RunMode = RunMode.Sync)]
        [Alias("g")]
        [Preconditions.RequireModerator]
        public async Task GameAsync(int gameNumber, int winningTeamNumber, SocketTextChannel lobbyChannel = null, [Remainder]string comment = null)
        {
            //TODO: Needs a way of cancelling games and calling draws
            if (winningTeamNumber != 1 && winningTeamNumber != 2)
            {
                await ReplyAsync("Team number must be either number `1` or `2`");
                return;
            }

            if (lobbyChannel == null)
            {
                //If no lobby is provided, assume that it is the current channel.
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, lobbyChannel.Id);
            if (lobby == null)
            {
                //Reply error not a lobby.
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            var game = Service.GetGame(Context.Guild.Id, lobby.ChannelId, gameNumber);
            if (game == null)
            {
                //Reply not valid game number.
                await ReplyAsync($"GameID is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            if (game.GameState == GameResult.State.Decided || game.GameState == GameResult.State.Draw)
            {
                await ReplyAsync("Game results cannot currently be overwritten without first running the `undogame` command.");
                return;
            }

            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);

            List < (Player, int, Rank, RankChangeState, Rank) > winList;
            List < (Player, int, Rank, RankChangeState, Rank) > loseList;
            if (winningTeamNumber == 1)
            {
                winList = UpdateTeamScoresAsync(competition, true, game.Team1.Players);
                loseList = UpdateTeamScoresAsync(competition, false, game.Team2.Players);
            }
            else
            {
                loseList = UpdateTeamScoresAsync(competition, false, game.Team1.Players);
                winList = UpdateTeamScoresAsync(competition, true, game.Team2.Players);
            }

            var allUsers = new List < (Player, int, Rank, RankChangeState, Rank) > ();
            allUsers.AddRange(winList);
            allUsers.AddRange(loseList);

            foreach (var user in allUsers)
            {
                //Ignore user updates if they aren't found in the server.
                var gUser = Context.Guild.GetUser(user.Item1.UserId);
                if (gUser == null) continue;

                #region ReducedUpdate
                await Service.UpdateUserAsync(competition, user.Item1, gUser);
                /*
                //Create the new user display name template
                var displayName = competition.GetNickname(user.Item1);

                //TODO: Check if the user can have their nickname set.
                bool nickNameUpdate = false;
                if (competition.UpdateNames && gUser.Nickname != null)
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
                            //Set the user's roles to the modified list which removes and lost ranks and adds any gained ranks
                            x.RoleIds = roleIds.Where(r => r != Context.Guild.EveryoneRole.Id).ToArray();
                        }
                    });
                }
                */
                #endregion
            }
            
            game.GameState = GameResult.State.Decided;
            game.ScoreUpdates = allUsers.ToDictionary(x => x.Item1.UserId, y => y.Item2);
            game.WinningTeam = winningTeamNumber;
            game.Comment = comment;
            game.Submitter = Context.User.Id;
            Service.SaveGame(game);

            var winField = new EmbedFieldBuilder
            {
                //TODO: Is this necessary to show which team the winning team was?
                Name = $"Winning Team, Team{winningTeamNumber}",
                Value = GetResponseContent(winList).FixLength(1023)
            };
            var loseField = new EmbedFieldBuilder
            {
                Name = $"Losing Team",
                Value = GetResponseContent(loseList).FixLength(1023)
            };
            var response = new EmbedBuilder
            {
                Fields = new List<EmbedFieldBuilder> { winField, loseField },
                //TODO: Remove this if from the vote command
                Title = $"GameID: {gameNumber} Result called by {Context.User.Username}#{Context.User.Discriminator}"
            };

            if (!string.IsNullOrWhiteSpace(comment))
            {
                response.AddField("Comment", comment.FixLength(1023));
            }

            await AnnounceResultAsync(lobby, response);
        }

        public string GetResponseContent(List < (Player, int, Rank, RankChangeState, Rank) > players)
        {
            var sb = new StringBuilder();
            foreach (var player in players)
            {
                if (player.Item4 == RankChangeState.None) 
                {
                    sb.AppendLine($"{player.Item1.DisplayName} **Points:** {player.Item1.Points} **Points Received:** {player.Item2}");
                    continue;
                }

                string originalRole = null;
                string newRole = null;
                if (player.Item3 != null)
                {
                    originalRole = MentionUtils.MentionRole(player.Item3.RoleId);
                }

                if (player.Item5 != null)
                {
                    newRole = MentionUtils.MentionRole(player.Item5.RoleId);
                }

                sb.AppendLine($"{player.Item1.DisplayName} **Points:** {player.Item1.Points} **Points Received:** {player.Item2} Rank: {originalRole ?? "N.A"} => {newRole ?? "N/A"}");

            }

            return sb.ToString();
        }

        public enum RankChangeState
        {
            DeRank,
            RankUp,
            None
        }

        //returns a list of userIds and the amount of points they received/lost for the win/loss, and if the user lost/gained a rank
        //UserId, Points added/removed, rank before, rank modify state, rank after
        /// <summary>
        /// Retrieves and updates player scores/wins
        /// </summary>
        /// <returns>
        /// A list containing a value tuple with the
        /// Player object
        /// Amount of points received/lost
        /// The player's current rank
        /// The player's rank change state (rank up, derank, none)
        /// The players new rank (if changed)
        /// </returns>
        public List < (Player, int, Rank, RankChangeState, Rank) > UpdateTeamScoresAsync(CompetitionConfig competition, bool win, HashSet<ulong> userIds)
        {
            var updates = new List < (Player, int, Rank, RankChangeState, Rank) > ();
            foreach (var userId in userIds)
            {
                var player = Service.GetPlayer(Context.Guild.Id, userId);
                if (player == null) continue;

                //This represents the current user's rank
                var maxRank = competition.MaxRank(player.Points);

                int updateVal;
                RankChangeState state = RankChangeState.None;
                Rank newRank = null;

                if (win)
                {
                    updateVal = maxRank?.WinModifier ?? competition.DefaultWinModifier;
                    player.SetPoints(competition, player.Points + updateVal);
                    player.Wins++;
                    newRank = competition.MaxRank(player.Points);
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
                    //Loss modifiers are always positive values that are to be subtracted
                    updateVal = maxRank?.LossModifier ?? competition.DefaultLossModifier;
                    player.SetPoints(competition, player.Points - updateVal);
                    player.Losses++;
                    //Set the update value to a negative value for returning purposes.
                    updateVal = -updateVal;

                    if (maxRank != null)
                    {
                        if (player.Points < maxRank.Points)
                        {
                            state = RankChangeState.DeRank;
                            newRank = competition.MaxRank(player.Points);
                        }
                    }
                }

                updates.Add((player, updateVal, maxRank, state, newRank));

                //TODO: Rank checking?
                //I forget what this means honestly
                Service.SavePlayer(player);
            }

            return updates;
        }
    }
}