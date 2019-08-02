using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.ELO.Modules.Preconditions;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [IsRegistered]
    [IsLobby]
    public class LobbyManagement : ELOBase
    {
        public LobbyManagement(Random random)
        {
            Random = random;
        }

        //TODO: Player queuing via reactions to a message.

        public Random Random { get; }

        //TODO: Replace command
        //TODO: Map stuff
        //TODO: Assign teams to temp roles until game result is decided.
        //TODO: Assign a game to a specific channel until game result is decided.
        //TODO: Allow players to party up for a lobby

        [Command("Lobby")]
        public async Task LobbyInfoAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = Color.Blue
            };
            embed.Description = $"**Pick Mode:** {Context.CurrentLobby.TeamPickMode}\n" 
                                + $"**Minimum Points to Queue:** {Context.CurrentLobby.MinimumPoints?.ToString() ?? "N/A"}\n"
                                + $"**Games Played:** {Context.CurrentLobby.CurrentGameCount}\n"
                                + $"**Players Per Team:** {Context.CurrentLobby.PlayersPerTeam}\n"
                                + $"**Maps:** {string.Join(", ", Context.CurrentLobby.Maps)}\n"
                                + "For Players in Queue use the `Queue` or `Q` Command.";
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Queue")]
        [Alias("Q")]
        public async Task ShowQueueAsync()
        {
            var game = Context.GetCurrentGame();
            if (game != null)
            {
                if (game.GameState == Models.GameResult.State.Picking)
                {
                    var gameEmbed = new EmbedBuilder
                    {
                        Title = $"Current Teams."
                    };
                    
                    var t1Users = GetMentionList(GetUserList(Context.Guild, game.Team1.Players));
                    var t2Users = GetMentionList(GetUserList(Context.Guild, game.Team2.Players));
                    var remainingPlayers = GetMentionList(GetUserList(Context.Guild, game.Queue.Where(x => !game.Team1.Players.Contains(x) && !game.Team2.Players.Contains(x))));
                    gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\n{string.Join("\n", t1Users)}");
                    gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\n{string.Join("\n", t2Users)}");
                    gameEmbed.AddField("Remaining Players", string.Join("\n", remainingPlayers));
                    await ReplyAsync("", false, gameEmbed.Build());
                    return;
                }
            }

            if (Context.CurrentLobby.Queue.Count > 0)
            {
                var mentionList = GetMentionList(GetUserList(Context.Guild, Context.CurrentLobby.Queue));
                var embed = new EmbedBuilder();
                embed.Title = $"{Context.Channel.Name} [{Context.CurrentLobby.Queue.Count}/{Context.CurrentLobby.PlayersPerTeam*2}]";
                embed.Description = $"Game: #{Context.CurrentLobby.CurrentGameCount}\n" 
                                    + string.Join("\n", mentionList);
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                await ReplyAsync("", false, "The queue is empty.".QuickEmbed());
            }
        }

        [Command("Join", RunMode = RunMode.Sync)]
        [Alias("JoinLobby", "Join Lobby", "j")]
        public async Task JoinLobbyAsync()
        {
            if (Context.CurrentLobby.Queue.Count >= Context.CurrentLobby.PlayersPerTeam * 2)
            {
                //Queue will be reset after teams are completely picked.
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

            var currentGame = Context.GetCurrentGame();
            if (currentGame != null)
            {
                if (currentGame.GameState == Models.GameResult.State.Picking)
                {
                    await ReplyAsync("Current game is picking teams, wait until this is completed.");
                    return;
                }
            }

            Context.CurrentLobby.Queue.Add(Context.User.Id);
            if (Context.CurrentLobby.Queue.Count >= Context.CurrentLobby.PlayersPerTeam * 2)
            {
                await ReplyAsync("Queue is full. Picking teams...");
                //Increment the game counter as there is now a new game.
                Context.CurrentLobby.CurrentGameCount += 1;
                var game = new GameResult(Context.CurrentLobby.CurrentGameCount, Context.Channel.Id, Context.Guild.Id);
                game.Queue = Context.CurrentLobby.Queue;
                Context.CurrentLobby.Queue = new List<ulong>();
                

                //Set team players/captains based on the team pick mode
                switch (Context.CurrentLobby.TeamPickMode)
                {
                    case Lobby.PickMode.Captains_HighestRanked:
                    case Lobby.PickMode.Captains_Random:
                    case Lobby.PickMode.Captains_RandomHighestRanked:
                        game.GameState = GameResult.State.Picking;
                        var captains = Context.Service.GetCaptains(Context.CurrentLobby, game, Random);
                        game.Team1.Captain = captains.Item1;
                        game.Team2.Captain = captains.Item2;
                        //TODO: Timer from when captains are mentioned to first pick time. Cancel game if command is not run.
                        await ReplyAsync($"Captains have been picked. Use the `pick` or `p` command to choose your players.\nCaptain 1: <@{game.Team1.Captain}>\nCaptain 2: <@{game.Team2.Captain}>");
                        break;
                    case Lobby.PickMode.Random:
                        game.GameState = GameResult.State.Undecided;
                        var shuffled = game.Queue.OrderBy(x => Random.Next()).ToList();
                        game.Team1.Players = shuffled.Take(Context.CurrentLobby.PlayersPerTeam).ToList();
                        game.Team2.Players = shuffled.Skip(Context.CurrentLobby.PlayersPerTeam).Take(Context.CurrentLobby.PlayersPerTeam).ToList();
                        break;
                    case Lobby.PickMode.TryBalance:
                        game.GameState = GameResult.State.Undecided;
                        var ordered = game.Queue.Select(x => Context.Service.GetPlayer(Context.Guild.Id, x)).Where(x => x != null).OrderByDescending(x => x.Points).ToList();
                        foreach (var user in ordered)
                        {
                            if (game.Team1.Players.Count > game.Team2.Players.Count)
                            {
                                game.Team2.Players.Add(user.UserId);
                            }
                            else
                            {
                                game.Team1.Players.Add(user.UserId);
                            }
                        }
                        break;
                }

                //TODO: Assign team members to specific roles and create a channel for chat within.
                if (Context.CurrentLobby.TeamPickMode == Lobby.PickMode.TryBalance || Context.CurrentLobby.TeamPickMode == Lobby.PickMode.Random)
                {
                    var t1Users = GetMentionList(GetUserList(Context.Guild, game.Team1.Players));
                    var t2Users = GetMentionList(GetUserList(Context.Guild, game.Team2.Players));
                    var gameEmbed = new EmbedBuilder
                    {
                        Title = $"Game #{game.GameId} Started"
                    };
                
                    gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\n{string.Join("\n", t1Users)}");
                    gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\n{string.Join("\n", t2Users)}");
                    await ReplyAsync("", false, gameEmbed.Build());
                }

                Context.Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
            }

            Context.Service.Database.Store(Context.CurrentLobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
        }

        [Command("Leave", RunMode = RunMode.Sync)]
        [Alias("LeaveLobby", "Leave Lobby", "l")]
        public async Task LeaveLobbyAsync()
        {
            if (Context.CurrentLobby.Queue.Contains(Context.User.Id))
            {
                var game = Context.GetCurrentGame();
                if (game != null)
                {
                    if (game.GameState == GameResult.State.Picking)
                    {
                        await ReplyAsync("Lobby is currently picking teams. You cannot leave a queue while this is happening.");
                        return;
                    }
                }
                Context.CurrentLobby.Queue.Remove(Context.User.Id);
                Context.Service.Database.Store(Context.CurrentLobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
                await ReplyAsync("Removed.");
            }
            else
            {
                await ReplyAsync("You are not queued in this lobby.");
            }
        }

        [Command("Pick", RunMode = RunMode.Sync)]
        [Alias("p")]
        public async Task PickPlayerAsync(params SocketGuildUser[] users)
        {
            var game = Context.GetCurrentGame();
            if (game.GameState != GameResult.State.Picking)
            {
                await ReplyAsync("Lobby is currently not picking teams.");
                return;
            }

            if (game.Team1.Captain != Context.User.Id && game.Team2.Captain != Context.User.Id)
            {
                await ReplyAsync("You are not a team captain.");
                return;
            }

            
            var userCount = users.Count();
            if (userCount == 0)
            {
                await ReplyAsync("You must specify a player to join.");
                return;
            } 
            else if (userCount > 2)
            {
                await ReplyAsync("Too many players specified.");
                return;
            }

            //Ensure that two players are specified for the first pick.
            //Or only one player if it is after the first pick.
            if (game.Team1.Players.Count == 0 || game.Team2.Players.Count == 0)
            {
                if (userCount != 2)
                {
                    await ReplyAsync("Please specify two players to be added on the first pick.");
                    return;
                }
            }
            else
            {
                if (userCount != 1)
                {
                    await ReplyAsync("Please specify only one player to be added to your team.");
                    return;
                }
            }


            if (!users.All(user => game.Queue.Contains(user.Id)))
            {
                await ReplyAsync("A selected player is not queued for this game.");
                return;
            }
            else if (users.Any(u => game.Team1.Players.Contains(u.Id) || game.Team2.Players.Contains(u.Id)))
            {
                await ReplyAsync("A selected player is already picked for a team.");
                return;
            }

            if (game.Team1.Players.Count == 0)
            {
                if (game.Team1.Captain != Context.User.Id)
                {
                    await ReplyAsync("You are not the team 1 captain.");
                    return;
                }

                game.Team1.Players.AddRange(users.Select(x => x.Id));
                game.Team1.Players.Add(game.Team1.Captain);
            }
            else if (game.Team2.Players.Count == 0)
            {
                if (game.Team2.Captain != Context.User.Id)
                {
                    await ReplyAsync("You are not the team 2 captain.");
                    return;
                }

                game.Team2.Players.AddRange(users.Select(x => x.Id));
                game.Team2.Players.Add(game.Team2.Captain);
            }
            else
            {
                //After both teams have picked their first two players, alternate between teams
                var user = users.First();

                if (game.Team1.Players.Count > game.Team2.Players.Count)
                {
                    if (game.Team2.Captain != Context.User.Id)
                    {
                        await ReplyAsync("You are not the team 2 captain.");
                        return;
                    }

                    game.Team2.Players.Add(user.Id);
                }
                else
                {
                    if (game.Team1.Captain != Context.User.Id)
                    {
                        await ReplyAsync("You are not the team 1 captain.");
                        return;
                    }

                    game.Team1.Players.Add(user.Id);
                }                
            }

            if (game.Team1.Players.Count + game.Team2.Players.Count >= game.Queue.Count)
            {
                //Teams have been filled.
                //TODO: Announce game
                game.GameState = GameResult.State.Undecided;
                //TODO: Map selection

                var gameEmbed = new EmbedBuilder
                {
                    Title = $"Game #{game.GameId} Started"
                };
                
                var t1Users = GetMentionList(GetUserList(Context.Guild, game.Team1.Players));
                var t2Users = GetMentionList(GetUserList(Context.Guild, game.Team2.Players));
                gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\n{string.Join("\n", t1Users)}");
                gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\n{string.Join("\n", t2Users)}");
                await ReplyAsync("", false, gameEmbed.Build());

            }
            else
            {
                //Display players in each team and remaining players.
                var gameEmbed = new EmbedBuilder
                {
                    Title = $"Player(s) picked."
                };
                
                var t1Users = GetMentionList(GetUserList(Context.Guild, game.Team1.Players));
                var t2Users = GetMentionList(GetUserList(Context.Guild, game.Team2.Players));
                var remainingPlayers = GetMentionList(GetUserList(Context.Guild, game.Queue.Where(x => !game.Team1.Players.Contains(x) && !game.Team2.Players.Contains(x))));
                gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\n{string.Join("\n", t1Users)}");
                gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\n{string.Join("\n", t2Users)}");
                gameEmbed.AddField("Remaining Players", string.Join("\n", remainingPlayers));
                await ReplyAsync("", false, gameEmbed.Build());
            }

            Context.Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
        }

        public SocketGuildUser[] GetUserList(SocketGuild guild, IEnumerable<ulong> userIds)
        {
            return userIds.Select(x => guild.GetUser(x)).ToArray();
        }

        public string[] GetMentionList(IEnumerable<SocketGuildUser> users)
        {
            return users.Select(x => x?.Mention ?? $"[{x.Id}]").ToArray();
        }

        //TODO: if more than x maps are added to the lobby, announce 3 (or so) and allow users to vote on them to pick
        //Would have 1 minute timeout, then either picks the most voted map or randomly chooses from the most voted.
        //Would need to have a way of reducing the amount of repeats as well.
    }
}