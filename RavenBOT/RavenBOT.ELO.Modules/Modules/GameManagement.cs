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
    [Preconditions.RequireModerator]
    public class GameManagement : ReactiveBase
    {
        public ELOService Service { get; }

        public GameManagement(ELOService service)
        {
            Service = service;
        }

        //TODO: Ensure correct commands require mod/admin perms

        //GameResult (Allow players to vote on result), needs to be an optionla command, requires both team captains to vote
        //Game (Mods/admins submit game results), could potentially accept a comment for the result as well (ie for proof of wins)
        //UndoGame (would need to use the amount of points added to the user rather than calculate at command run time)

        [Command("UndoGame", RunMode = RunMode.Sync)]
        [Alias("Undo Game")]
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

        public async Task UpdateScores(Lobby lobby, GameResult game, CompetitionConfig competition)
        {
            foreach (var score in game.UpdatedScores)
            {
                var player = Service.GetPlayer(Context.Guild.Id, score.Item1);
                if (player == null)
                {
                    //Skip if for whatever reason the player profile cannot be found.
                    continue;
                }

                var currentRank = competition.MaxRank(player.Points);

                if (score.Item2 < 0)
                {
                    //Points lost, so add them back
                    player.Losses--;
                    player.Points += Math.Abs(score.Item2);
                }
                else
                {
                    //Points gained so remove them
                    player.Wins--;
                    player.Points -= score.Item2;
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
                if (guildUser.Nickname != null)
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
                                x.RoleIds = currentRoles.ToArray();
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
            game.UpdatedScores = new HashSet <(ulong, int)> ();
            Service.SaveGame(game);
        }

        [Command("DeleteGame", RunMode = RunMode.Sync)]
        [Alias("Delete Game", "DelGame")]
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

        [Command("Draw", RunMode = RunMode.Sync)]
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
            game.UpdatedScores = new HashSet<(ulong, int)>();
            Service.SaveGame(game);

            await DrawPlayersAsync(game.Team1.Players);
            await DrawPlayersAsync(game.Team2.Players);
            await ReplyAsync($"Called draw on game #{game.GameId}, player's game and draw counts have been updated.");
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
        public async Task GameAsync(int winningTeamNumber, int gameNumber, SocketTextChannel lobbyChannel = null, [Remainder]string comment = null)
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

                //Create the new user display name template
                var displayName = competition.GetNickname(user.Item1);

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
                            //Set the user's roles to the modified list which removes and lost ranks and adds any gained ranks
                            x.RoleIds = roleIds.ToArray();
                        }
                    });
                }
            }

            game.GameState = GameResult.State.Decided;
            game.UpdatedScores = allUsers.Select(x => (x.Item1.UserId, x.Item2)).ToHashSet();
            game.WinningTeam = winningTeamNumber;
            game.Comment = comment;
            game.Submitter = Context.User.Id;
            Service.SaveGame(game);

            var winField = new EmbedFieldBuilder
            {
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
                Title = $"GameID: {gameNumber} Result called by {Context.User.Username}#{Context.User.Discriminator}"
            };

            if (!string.IsNullOrWhiteSpace(comment))
            {
                response.AddField("Comment", comment.FixLength(1023));
            }

            await ReplyAsync("", false, response.Build());
        }

        public string GetResponseContent(List < (Player, int, Rank, RankChangeState, Rank) > players)
        {
            var sb = new StringBuilder();
            foreach (var player in players)
            {
                if (player.Item4 == RankChangeState.None) 
                {
                    sb.AppendLine($"[{player.Item1.Points}] {player.Item1.DisplayName} Points: {player.Item2}");
                    continue;
                }

                SocketRole originalRole = null;
                SocketRole newRole = null;
                if (player.Item3 != null)
                {
                    originalRole = Context.Guild.GetRole(player.Item3.RoleId);
                }

                if (player.Item5 != null)
                {
                    newRole = Context.Guild.GetRole(player.Item5.RoleId);
                }

                sb.AppendLine($"[{player.Item1.Points}] {player.Item1.DisplayName} Points: {player.Item2} Rank: {originalRole?.Mention ?? "N.A"} => {newRole?.Mention ?? "N/A"}");

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
                    player.Points += updateVal;
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
                    //Ensure the update value is positive as it will be subtracted from the user's points.
                    updateVal = Math.Abs(maxRank?.LossModifier ?? competition.DefaultLossModifier);
                    player.Points -= updateVal;
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