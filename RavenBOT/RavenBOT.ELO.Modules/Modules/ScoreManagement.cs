using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    //TODO: Moderator permission instead of just admin
    [Preconditions.RequireModerator]
    public class ScoreManagement : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public ScoreManagement(ELOService service)
        {
            Service = service;
        }

        [Command("ModifyStates")]
        public async Task ModifyStatesAsync()
        {
            var states = Extensions.ConvertEnumToDictionary<Player.ModifyState>();
            await ReplyAsync(string.Join("\n", states.Select(x => x.Key)));
        }

        //TODO: Consider whether it's necessary to have the single user command as multi user already is able to accept only one.
        [Command("Points", RunMode = RunMode.Sync)]
        public async Task PointsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await PointsAsync(state, amount, user);
        }

        [Command("Points", RunMode = RunMode.Sync)]
        public async Task PointsAsync(Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var newVal = Player.ModifyValue(state, player.Points, amount);
                responseString += $"{player.DisplayName}: {player.Points} => {newVal}\n";
                player.Points = newVal;
            }
            Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }
        
        [Command("Wins", RunMode = RunMode.Sync)]
        public async Task WinsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await WinsAsync(state, amount, user);
        }

        [Command("Wins", RunMode = RunMode.Sync)]
        public async Task WinsAsync(Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var newVal = Player.ModifyValue(state, player.Wins, amount);
                responseString += $"{player.DisplayName}: {player.Wins} => {newVal}\n";
                player.Wins = newVal;
            }
            Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }

        [Command("Losses", RunMode = RunMode.Sync)]
        public async Task LossesAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await LossesAsync(state, amount, user);
        }

        [Command("Losses", RunMode = RunMode.Sync)]
        public async Task LossesAsync(Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var newVal = Player.ModifyValue(state, player.Losses, amount);
                responseString += $"{player.DisplayName}: {player.Losses} => {newVal}\n";
                player.Losses = newVal;
            }
            Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }

        [Command("Draws", RunMode = RunMode.Sync)]
        public async Task DrawsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await DrawsAsync(state, amount, user);
        }

        [Command("Draws", RunMode = RunMode.Sync)]
        public async Task DrawsAsync(Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var newVal = Player.ModifyValue(state, player.Draws, amount);
                responseString += $"{player.DisplayName}: {player.Draws} => {newVal}\n";
                player.Draws = newVal;
            }
            Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }

        [Command("PlayerModify", RunMode = RunMode.Sync)]
        public async Task PlayerModifyAsync(SocketGuildUser user, string value, Player.ModifyState state, int amount)
        {
            await PlayersModifyAsync(value, state, amount, user);
        }

        
        [Command("PlayersModify", RunMode = RunMode.Sync)]
        public async Task PlayersModifyAsync(string value, Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var response = player.UpdateValue(value, state, amount);
                responseString += $"{player.DisplayName}: {response.Item1} => {response.Item2}\n";
            }
            Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }
    }
}