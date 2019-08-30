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
    public partial class LobbyManagement : ReactiveBase
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

        [Command("ClearQueue", RunMode = RunMode.Sync)]
        [Alias("Clear Queue", "clearq", "clearque")]
        [Preconditions.RequireModerator]
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
            Service.SaveLobby(CurrentLobby);
            await ReplyAsync("Queue Cleared.");
        }

        [Command("ForceRemove")]
        [Preconditions.RequireModerator]
        public async Task ForceRemove(SocketGuildUser user)
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
                    await ReplyAsync("You cannot remove a player from a game that is still being picked, try cancelling the game instead.");
                    return;
                }                
            }

            if (CurrentLobby.Queue.Contains(user.Id))
            {
                CurrentLobby.Queue.Remove(user.Id);
                await ReplyAsync("Player was removed from queue.");
                Service.SaveLobby(CurrentLobby);
            }
            else
            {
                await ReplyAsync("Player is not in queue and cannot be removed.");
                return;
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

            //Ensure the player is eligible to join a team
            if (users.Any(user => !game.Queue.Contains(user.Id)))
            {
                await ReplyAsync("A selected player is not queued for this game.");
                return;
            }
            else if (users.Any(u => game.Team1.Players.Contains(u.Id) || game.Team2.Players.Contains(u.Id)))
            {
                await ReplyAsync("A selected player is already picked for a team.");
                return;
            }
            else if (users.Any(u => u.Id == game.Team1.Captain || u.Id == game.Team2.Captain))
            {
                await ReplyAsync("You cannot select a captain for picking.");
                return;
            }

            if (game.PickOrder == GameResult.CaptainPickOrder.PickTwo)
            {
                game = await PickTwoAsync(game, users);
            }
            else if (game.PickOrder == GameResult.CaptainPickOrder.PickOne)
            {
                game = await PickOneAsync(game, users);
            }
            else
            {
                await ReplyAsync("There was an error picking your game.");
                return;
            }

            //game will be returned null from pickone/picktwo if there was an issue with a pick. The function already replies to just return.
            if (game == null)
            {
                return;
            }
            else
            {
                var remaining = game.GetQueueRemainingPlayers();
                if (remaining.Count() == 1)
                {
                    game.GetTeam().Players.Add(remaining.First());
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
                gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\nPlayers:\n{string.Join("\n", t1Users)}");
                gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\nPlayers:\n{string.Join("\n", t2Users)}");
                await ReplyAsync("", false, gameEmbed.Build());
                if (CurrentLobby.GameReadyAnnouncementChannel != 0)
                {
                    var channel = Context.Guild.GetTextChannel(CurrentLobby.GameReadyAnnouncementChannel);
                    if (channel != null)
                    {
                        await channel.SendMessageAsync("", false, gameEmbed.Build());
                    }
                }
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
                var remainingPlayers = GetMentionList(GetUserList(Context.Guild, RemainingPlayers(game)));
                gameEmbed.AddField("Team 1", $"Captain: {Context.Guild.GetUser(game.Team1.Captain)?.Mention ?? $"[{game.Team1.Captain}]"}\nPlayers:\n{string.Join("\n", t1Users)}");
                gameEmbed.AddField("Team 2", $"Captain: {Context.Guild.GetUser(game.Team2.Captain)?.Mention ?? $"[{game.Team2.Captain}]"}\nPlayers:\n{string.Join("\n", t2Users)}");
                gameEmbed.AddField("Remaining Players", string.Join("\n", remainingPlayers));
                await ReplyAsync(PickResponse ?? "", false, gameEmbed.Build());
            }

            Service.SaveGame(game);
        }

        //TODO: if more than x maps are added to the lobby, announce 3 (or so) and allow users to vote on them to pick
        //Would have 1 minute timeout, then either picks the most voted map or randomly chooses from the most voted.
        //Would need to have a way of reducing the amount of repeats as well.
    }
}