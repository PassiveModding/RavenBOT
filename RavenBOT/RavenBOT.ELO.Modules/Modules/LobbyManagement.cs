using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.ELO.Modules.Preconditions;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    [IsRegistered]
    [IsLobby]
    public class LobbyManagement : ELOBase
    {
        public LobbyManagement(Random random)
        {
            Random = random;
        }

        public Random Random { get; }

        [Command("Join")]
        [Alias("JoinLobby", "Join Lobby", "j")]
        public async Task JoinLobbyAsync()
        {
            if (Context.CurrentLobby.Queue.Count >= Context.CurrentLobby.PlayersPerTeam * 2)
            {
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
                var game = new GameResult(Context.CurrentLobby.CurrentGameCount + 1, Context.Channel.Id, Context.Guild.Id);
                game.Queue = Context.CurrentLobby.Queue;
                Context.CurrentLobby.Queue = new List<ulong>();
                
                switch (Context.CurrentLobby.TeamPickMode)
                {
                    case Lobby.PickMode.Captains:
                        game.GameState = GameResult.State.Picking;
                        var captains = Context.Service.GetCaptains(Context.CurrentLobby, game, Random);
                        game.Team1.Captain = captains.Item1;
                        game.Team2.Captain = captains.Item2;
                        //TODO: Ping team captains
                        //TODO: Timer from when captains are mentioned to first pick time. Cancel game if command is not run.
                        await ReplyAsync("Captains have been picked. Use the pick command to choose your players.");
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

                //TODO: Announce team members.
                Context.Service.Database.Store(game, GameResult.DocumentName(game.GameId, game.LobbyId, game.GuildId));
            }
            //TODO: Create game on lobby full.
            Context.Service.Database.Store(Context.CurrentLobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
        }

        [Command("Leave")]
        [Alias("LeaveLobby", "Leave Lobby", "l")]
        public async Task LeaveLobbyAsync()
        {


            //TODO: Check if game is picking players.
            //TODO: Create game on lobby full.
        }
    }
}