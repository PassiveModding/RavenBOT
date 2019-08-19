using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class LobbyManagement : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public LobbyManagement(ELOService service, Random random)
        {
            Service = service;
            Random = random;
        }

        public Lobby CurrentLobby;

        public async Task<bool> CheckLobbyAsync()
        {
            var response = Service.IsLobby(Context.Guild.Id, Context.Channel.Id);
            if (response.Item1)
            {
                CurrentLobby = response.Item2;
                return true;
            }

            await ReplyAsync("Current channel is not a lobby.");
            return false;            
        }

        public Player CurrentPlayer;

        public async Task<bool> CheckRegisteredAsync()
        {
            var response = Service.GetPlayer(Context.Guild.Id, Context.User.Id);
            if (response != null)
            {
                CurrentPlayer = response;
                return true;
            }

            await ReplyAsync("You are not registered");
            return false;            
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
            if (!await CheckLobbyAsync() || !await CheckRegisteredAsync())
            {
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = Color.Blue
            };
            embed.Description = $"**Pick Mode:** {CurrentLobby.TeamPickMode}\n" 
                                + $"**Minimum Points to Queue:** {CurrentLobby.MinimumPoints?.ToString() ?? "N/A"}\n"
                                + $"**Games Played:** {CurrentLobby.CurrentGameCount}\n"
                                + $"**Players Per Team:** {CurrentLobby.PlayersPerTeam}\n"
                                + $"**Maps:** {string.Join(", ", CurrentLobby.Maps)}\n"
                                + "For Players in Queue use the `Queue` or `Q` Command.";
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Clear Queue")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearQueueAsync()
        {
            if (!await CheckLobbyAsync())
            {
                return;
            }

            var game = Service.GetCurrentGame(CurrentLobby);
            if (game != null)
            {
                if (game.GameState == GameResult.State.Picking)
                {
                    await ReplyAsync("Current game is being picked, cannot clear queue.");
                    return;
                }
            }
            CurrentLobby.Queue.Clear();
            Service.Database.Store(CurrentLobby, Lobby.DocumentName(CurrentLobby.GuildId, CurrentLobby.ChannelId));
            await ReplyAsync("Queue Cleared.");
        }

        [Command("Queue")]
        [Alias("Q")]
        public async Task ShowQueueAsync()
        {
            if (!await CheckLobbyAsync())
            {
                return;
            }

            var game = Service.GetCurrentGame(CurrentLobby);
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

            if (CurrentLobby.Queue.Count > 0)
            {
                var mentionList = GetMentionList(GetUserList(Context.Guild, CurrentLobby.Queue));
                var embed = new EmbedBuilder();
                embed.Title = $"{Context.Channel.Name} [{CurrentLobby.Queue.Count}/{CurrentLobby.PlayersPerTeam*2}]";
                embed.Description = $"Game: #{CurrentLobby.CurrentGameCount}\n" 
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
            if (!await CheckLobbyAsync() || !await CheckRegisteredAsync())
            {
                return;
            }

            if (CurrentLobby.Queue.Count >= CurrentLobby.PlayersPerTeam * 2)
            {
                //Queue will be reset after teams are completely picked.
                await ReplyAsync("Queue is full, wait for teams to be chosen before joining.");
                return;
            }

            if (Service.GetCompetition(Context.Guild.Id).BlockMultiQueueing)
            {
                var lobbies = Service.GetLobbies(Context.Guild.Id);
                var lobbyMatches = lobbies.Where(x => x.Queue.Contains(Context.User.Id));
                if (lobbyMatches.Any())
                {
                    var guildChannels = lobbyMatches.Select(x => Context.Guild.GetTextChannel(x.ChannelId)?.Mention ?? $"[{x.ChannelId}]");
                    await ReplyAsync($"MultiQueuing is not enabled in this server.\nPlease leave: {string.Join("\n", guildChannels)}");
                    return;
                }
            }

            var currentGame = Service.GetCurrentGame(CurrentLobby);
            if (currentGame != null)
            {
                if (currentGame.GameState == Models.GameResult.State.Picking)
                {
                    await ReplyAsync("Current game is picking teams, wait until this is completed.");
                    return;
                }
            }

            if (CurrentLobby.Queue.Contains(Context.User.Id))
            {
                await ReplyAndDeleteAsync("You are already queued.");
                return;
            }

            CurrentLobby.Queue.Add(Context.User.Id);
            if (CurrentLobby.Queue.Count >= CurrentLobby.PlayersPerTeam * 2)
            {
                await ReplyAsync("Queue is full. Picking teams...");
                //Increment the game counter as there is now a new game.
                CurrentLobby.CurrentGameCount += 1;
                var game = new GameResult(CurrentLobby.CurrentGameCount, Context.Channel.Id, Context.Guild.Id);
                game.Queue = CurrentLobby.Queue;
                CurrentLobby.Queue = new HashSet<ulong>();
                

                if (CurrentLobby.PlayersPerTeam == 1 && 
                    (CurrentLobby.TeamPickMode == Lobby.PickMode.Captains_HighestRanked ||
                    CurrentLobby.TeamPickMode == Lobby.PickMode.Captains_Random ||
                    CurrentLobby.TeamPickMode == Lobby.PickMode.Captains_RandomHighestRanked))
                {
                    //Ensure that there isnt a captain pick mode if the teams only consist of one player
                    await ReplyAsync("Lobby sort mode was set to random, you cannot have a captain lobby for solo queues.");
                    CurrentLobby.TeamPickMode = Lobby.PickMode.Random;
                }

                //Set team players/captains based on the team pick mode
                switch (CurrentLobby.TeamPickMode)
                {
                    case Lobby.PickMode.Captains_HighestRanked:
                    case Lobby.PickMode.Captains_Random:
                    case Lobby.PickMode.Captains_RandomHighestRanked:
                        game.GameState = GameResult.State.Picking;
                        var captains = Service.GetCaptains(CurrentLobby, game, Random);
                        game.Team1.Captain = captains.Item1;
                        game.Team2.Captain = captains.Item2;

                        //TODO: Timer from when captains are mentioned to first pick time. Cancel game if command is not run.
                        await ReplyAsync($"Captains have been picked. Use the `pick` or `p` command to choose your players.\nCaptain 1: <@{game.Team1.Captain}>\nCaptain 2: <@{game.Team2.Captain}>");  
                        break;
                    case Lobby.PickMode.Random:
                        game.GameState = GameResult.State.Undecided;
                        var shuffled = game.Queue.OrderBy(x => Random.Next()).ToList();
                        game.Team1.Players = shuffled.Take(CurrentLobby.PlayersPerTeam).ToHashSet();
                        game.Team2.Players = shuffled.Skip(CurrentLobby.PlayersPerTeam).Take(CurrentLobby.PlayersPerTeam).ToHashSet();
                        break;
                    case Lobby.PickMode.TryBalance:
                        game.GameState = GameResult.State.Undecided;
                        var ordered = game.Queue.Select(x => Service.GetPlayer(Context.Guild.Id, x)).Where(x => x != null).OrderByDescending(x => x.Points).ToList();
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
                if (CurrentLobby.TeamPickMode == Lobby.PickMode.TryBalance || CurrentLobby.TeamPickMode == Lobby.PickMode.Random)
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

                Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
            }
            else
            {
                if (Context.Guild.CurrentUser.GuildPermissions.AddReactions)
                {
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                else
                {
                    await ReplyAsync("Added to queue.");
                }
            }

            Service.Database.Store(CurrentLobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
        }

        [Command("Leave", RunMode = RunMode.Sync)]
        [Alias("LeaveLobby", "Leave Lobby", "l")]
        public async Task LeaveLobbyAsync()
        {            
            if (!await CheckLobbyAsync() || !await CheckRegisteredAsync())
            {
                return;
            }

            if (CurrentLobby.Queue.Contains(Context.User.Id))
            {
                var game = Service.GetCurrentGame(CurrentLobby);
                if (game != null)
                {
                    if (game.GameState == GameResult.State.Picking)
                    {
                        await ReplyAsync("Lobby is currently picking teams. You cannot leave a queue while this is happening.");
                        return;
                    }
                }
                CurrentLobby.Queue.Remove(Context.User.Id);
                Service.Database.Store(CurrentLobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
                
                if (Context.Guild.CurrentUser.GuildPermissions.AddReactions)
                {
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                else
                {
                    await ReplyAsync("Removed from queue.");
                }
            }
            else
            {
                await ReplyAsync("You are not queued for the next game.");
            }
        }

        [Command("Pick", RunMode = RunMode.Sync)]
        [Alias("p")]
        public async Task PickPlayerAsync(params SocketGuildUser[] users)
        {
            if (!await CheckLobbyAsync() || !await CheckRegisteredAsync())
            {
                return;
            }

            var game = Service.GetCurrentGame(CurrentLobby);
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

                game.Team1.Players.UnionWith(users.Select(x => x.Id));
                game.Team1.Players.Add(game.Team1.Captain);
            }
            else if (game.Team2.Players.Count == 0)
            {
                if (game.Team2.Captain != Context.User.Id)
                {
                    await ReplyAsync("You are not the team 2 captain.");
                    return;
                }

                game.Team2.Players.UnionWith(users.Select(x => x.Id));
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

            Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
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