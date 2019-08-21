using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Models;
using Discord;
using RavenBOT.ELO.Modules.Methods;
using Discord.Addons.Interactive;
using Newtonsoft.Json;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class GameManagement : InteractiveBase<ShardedCommandContext>
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

        [Command("UndoGame")]
        [Alias("Undo Game")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task UndoGameAsync(int gameNumber, SocketTextChannel lobbyChannel = null)
        {
            if (lobbyChannel == null)
            {
                lobbyChannel = Context.Channel as SocketTextChannel;
            }

            var competition = Service.GetCompetition(Context.Guild.Id);
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
                await ReplyAsync($"Game number is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            if (game.GameState != GameResult.State.Decided)
            {
                await ReplyAsync("Game result is not decided. NOTE: Draw results cannot currently be undone.");
                return;
            }

            foreach (var score in game.UpdatedScores)
            {
                var player = Service.GetPlayer(Context.Guild.Id, score.Item1);
                if (player == null)
                {
                    //Skip if for whatever reason the player profile cannot be found.
                    continue;
                }

                var currentRank = MaxRank(competition, player.Points);

                if (score.Item2 < 0)
                {
                    //Points lost, so add them back
                    player.Points += Math.Abs(score.Item2);
                }
                else
                {
                    //Points gained so remove them
                    player.Points -= score.Item2;
                }

                //Save the player profile after updating scores.
                Service.Database.Store(player, Player.DocumentName(player.GuildId, player.UserId));

                var guildUser = Context.Guild.GetUser(player.UserId);
                if (guildUser == null)
                {
                    //The user cannot be found in the server so skip updating their name/profile
                    continue;
                }

                var displayName = $"[{player.Points}] - {player.DisplayName}".FixLength(32);
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
                var newRank = MaxRank(competition, player.Points);
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
            game.UpdatedScores = new HashSet<(ulong, int)>();
            Service.Database.Store(game, GameResult.DocumentName(game.GameId, lobby.ChannelId, lobby.GuildId));
            //TODO: Announce the undone game
        }

        [Command("DeleteGame")]
        [Alias("DelGame", "Delete Game")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
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
                await ReplyAsync("Invalid game number.");
                return;
            }

            Service.Database.Remove<GameResult>(GameResult.DocumentName(gameNumber, lobbyChannel.Id, Context.Guild.Id));
            await ReplyAsync("Game Deleted.", false, JsonConvert.SerializeObject(game, Formatting.Indented).FixLength(2047).QuickEmbed());
        }

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
                await ReplyAsync($"Game number is invalid. Most recent game is {lobby.CurrentGameCount}");
                return;
            }

            var competition = Service.GetCompetition(Context.Guild.Id);
            
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

                //Create the new user display name template
                var displayName = $"[{user.Item1.Points}] - {user.Item1.DisplayName}".FixLength(32);

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
            game.WinningTeam = teamNumber;
            Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));

            var winField = new EmbedFieldBuilder
            {
                Name = $"Winning Team, Team #{teamNumber}",
                Value = GetResponseContent(winList).FixLength(1023)
            };
            var loseField = new EmbedFieldBuilder
            {
                Name = $"Losing Team",
                Value = GetResponseContent(loseList).FixLength(1023)
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
        public List<(Player, int, Rank, RankChangeState, Rank)> UpdateTeamScoresAsync(CompetitionConfig competition, bool win, HashSet<ulong> userIds)
        {
            var updates = new List<(Player, int, Rank, RankChangeState, Rank)>();
            foreach (var userId in userIds)
            {
                var botUser = Service.GetPlayer(Context.Guild.Id, userId);
                if (botUser == null) continue;

                //This represents the current user's rank
                var maxRank = MaxRank(competition, botUser.Points);

                int updateVal;
                RankChangeState state = RankChangeState.None;
                Rank newRank = null;

                //TODO: Add support for draw tracking
                botUser.Games++;

                if (win)
                {
                    updateVal = maxRank?.WinModifier ?? competition.DefaultWinModifier;
                    botUser.Points += updateVal;
                    botUser.Wins++;
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
                    //Ensure the update value is positive as it will be subtracted from the user's points.
                    updateVal = Math.Abs(maxRank?.LossModifier ?? competition.DefaultLossModifier);
                    botUser.Points -= updateVal;
                    botUser.Losses++;
                    //Set the update value to a negative value for returning purposes.
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

                //TODO: Rank checking?
                //I forget what this means honestly
                Service.Database.Store(botUser, Player.DocumentName(botUser.GuildId, botUser.UserId));
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