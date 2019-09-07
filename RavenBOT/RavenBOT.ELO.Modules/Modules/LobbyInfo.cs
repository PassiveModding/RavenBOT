using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;

namespace RavenBOT.ELO.Modules.Modules
{
    public partial class LobbyManagement
    {
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
            
            string maps;
            if (CurrentLobby.MapSelector != null)
            {
                maps = $"\n**Map Mode:** {CurrentLobby.MapSelector.Mode}\n" +
                        $"**Maps:** {string.Join(", ", CurrentLobby.MapSelector.Maps)}\n" +
                        $"**Recent Maps:** {string.Join(", ", CurrentLobby.MapSelector.GetHistory())}";
            }
            else
            {
                maps = "N/A";
            }

            embed.Description = $"**Pick Mode:** {CurrentLobby.TeamPickMode}\n" +
                $"**Minimum Points to Queue:** {CurrentLobby.MinimumPoints?.ToString() ?? "N/A"}\n" +
                $"**Games Played:** {CurrentLobby.CurrentGameCount}\n" +
                $"**Players Per Team:** {CurrentLobby.PlayersPerTeam}\n" +
                $"**Map Info:** {maps}\n" +
                "For Players in Queue use the `Queue` or `Q` Command.";
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Queue")]
        [Alias("Q", "lps", "listplayers", "playerlist", "who")]
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
                embed.Description = $"Game: #{CurrentLobby.CurrentGameCount}\n" +
                    string.Join("\n", mentionList);
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                await ReplyAsync("", false, "The queue is empty.".QuickEmbed());
            }
        }
    }
}