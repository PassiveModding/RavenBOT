using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    public partial class LobbyManagement
    {
        public async Task LobbyFullAsync()
        {
            await ReplyAsync("Queue is full. Picking teams...");
            //Increment the game counter as there is now a new game.
            CurrentLobby.CurrentGameCount += 1;
            var game = new GameResult(CurrentLobby.CurrentGameCount, Context.Channel.Id, Context.Guild.Id, CurrentLobby.TeamPickMode);
            game.Queue = CurrentLobby.Queue;
            foreach (var userId in game.Queue)
            {
                //TODO: Fetch and update players later as some could be retrieved later like in the captains function.
                var player = Service.GetPlayer(Context.Guild.Id, Context.User.Id);
                if (player == null) continue;
                player.AddGame(game.GameId);
                Service.SavePlayer(player);
            }

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
                    await ReplyAsync($"Captains have been picked. Use the `pick` or `p` command to choose your players.\nCaptain 1: {MentionUtils.MentionUser(game.Team1.Captain)}\nCaptain 2: {MentionUtils.MentionUser(game.Team2.Captain)}");
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

                //TODO: Is it necessary to announce captains here? since auto selected teams don't really have a captain
                //Maybe add an additional property for server owners to select
                gameEmbed.AddField("Team 1", $"Players: {string.Join("\n", t1Users)}");
                gameEmbed.AddField("Team 2", $"Players: {string.Join("\n", t2Users)}");

                //Display message mentioning all team members so they are pinged and notified for the game.
                var message = string.Join(" ", game.Queue.Select(x => MentionUtils.MentionUser(x)));
                await ReplyAsync(message, false, gameEmbed.Build());
                if (CurrentLobby.GameReadyAnnouncementChannel != 0)
                {
                    var channel = Context.Guild.GetTextChannel(CurrentLobby.GameReadyAnnouncementChannel);
                    if (channel != null)
                    {
                        await channel.SendMessageAsync(message, false, gameEmbed.Build());
                    }
                }
            }

            Service.SaveGame(game);
        }

        public async Task<GameResult> PickOneAsync(GameResult game, SocketGuildUser[] users)
        {
            var uc = users.Count();
            if (uc != 1)
            {
                await ReplyAsync("You can only pick one player at a time here.");
                return null;
            }

            var team = game.GetTeam();

            if (Context.User.Id != team.Captain)
            {
                await ReplyAsync("It is currently the other captains turn to pick.");
                return null;
            }

            if (uc != 1)
            {
                await ReplyAsync("You can only specify one player for this pick.");
                return null;
            }

            team.Players.Add(users.First().Id);
            PickResponse = $"{MentionUtils.MentionUser(game.GetOffTeam().Captain)} can select **1** player for the next pick.";
            game.Picks++;

            return game;
        }

        private string PickResponse = null;

        public async Task<GameResult> PickTwoAsync(GameResult game, SocketGuildUser[] users)
        {
            var uc = users.Count();
            //Lay out custom logic for 1-2-2-1-1... pick order.

            var team = game.GetTeam();
            var offTeam = game.GetOffTeam();

            if (game.Picks == 0)
            {
                //captain 1 turn to pick.
                if (Context.User.Id != team.Captain)
                {
                    await ReplyAsync("It is currently the team 1 captains turn to pick.");
                    return null;
                }

                if (uc != 1)
                {
                    await ReplyAsync("You can only specify one player for the initial pick.");
                    return null;
                }

                team.Players.Add(team.Captain);
                team.Players.Add(users.First().Id);
                game.Picks++;
                PickResponse = $"{MentionUtils.MentionUser(offTeam.Captain)} can select **2** players for the next pick.";
            }
            else if (game.Picks <= 2)
            {
                //cap 2 turn to pick. they get to pick 2 people.
                if (Context.User.Id != team.Captain)
                {
                    await ReplyAsync("It is currently the other captains turn to pick.");
                    return null;
                }

                if (uc != 2)
                {
                    await ReplyAsync("You must specify 2 players for this pick.");
                    return null;
                }

                //Note adding a player multiple times (ie team captain to team 1) will not affect it because the players are a hashset.
                team.Players.Add(team.Captain);
                team.Players.UnionWith(users.Select(x => x.Id));
                PickResponse = $"{MentionUtils.MentionUser(offTeam.Captain)} can select **2** players for the next pick.";
                game.Picks++;
            }
            else
            {
                if (Context.User.Id != team.Captain)
                {
                    await ReplyAsync("It is currently the other captains turn to pick.");
                    return null;
                }

                if (uc != 1)
                {
                    await ReplyAsync("You can only specify one player for this pick.");
                    return null;
                }

                team.Players.Add(users.First().Id);
                PickResponse = $"{MentionUtils.MentionUser(team.Captain)} can select **1** player for the next pick.";
                game.Picks++;
            }

            return game;
        }

        public ulong[] RemainingPlayers(GameResult game)
        {
            return game.Queue.Where(x => !game.Team1.Players.Contains(x) && !game.Team2.Players.Contains(x) &&
                x != game.Team1.Captain && x != game.Team2.Captain).ToArray();
        }

        public SocketGuildUser[] GetUserList(SocketGuild guild, IEnumerable<ulong> userIds)
        {
            return userIds.Select(x => guild.GetUser(x)).ToArray();
        }

        public string[] GetMentionList(IEnumerable<SocketGuildUser> users)
        {
            return users.Select(x => x?.Mention ?? $"[{x.Id}]").ToArray();
        }

    }
}