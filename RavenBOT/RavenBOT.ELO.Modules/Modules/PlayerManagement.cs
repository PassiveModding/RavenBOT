using System;
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
    [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
    public class PlayerManagement : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public PlayerManagement(ELOService service)
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
        [Command("Points")]
        public async Task PointsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await PointsAsync(state, amount, user);
        }

        [Command("Points")]
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

        [Command("PlayerModify")]
        public async Task PlayerModifyAsync(SocketGuildUser user, string value, Player.ModifyState state, int amount)
        {
            await PlayersModifyAsync(value, state, amount, user);
        }

        
        [Command("PlayersModify")]
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