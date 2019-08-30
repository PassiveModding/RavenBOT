using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    public partial class LobbyManagement
    {
        
        [Command("Join", RunMode = RunMode.Sync)]
        [Alias("JoinLobby", "Join Lobby", "j", "sign", "play", "ready")]
        public async Task JoinLobbyAsync()
        {
            if (!await CheckLobbyAsync() || !await CheckRegisteredAsync())
            {
                return;
            }

            //Not sure if this is actually needed.
            if (CurrentLobby.Queue.Count >= CurrentLobby.PlayersPerTeam * 2)
            {
                //Queue will be reset after teams are completely picked.
                await ReplyAsync("Queue is full, wait for teams to be chosen before joining.");
                return;
            }

            if (Service.GetOrCreateCompetition(Context.Guild.Id).BlockMultiQueueing)
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

            if (CurrentLobby.MinimumPoints != null)
            {
                if (CurrentPlayer.Points < CurrentLobby.MinimumPoints)
                {
                    await ReplyAsync($"You need a minimum of {CurrentLobby.MinimumPoints} points to join this lobby.");
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
                await ReplyAsync("You are already queued.");
                return;
            }

            CurrentLobby.Queue.Add(Context.User.Id);
            if (CurrentLobby.Queue.Count >= CurrentLobby.PlayersPerTeam * 2)
            {
                await LobbyFullAsync();
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

            Service.SaveLobby(CurrentLobby);
        }

        [Command("Leave", RunMode = RunMode.Sync)]
        [Alias("LeaveLobby", "Leave Lobby", "l", "out", "unsign", "remove", "unready")]
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
                Service.SaveLobby(CurrentLobby);

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

    }
}