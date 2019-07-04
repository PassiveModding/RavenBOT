using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    //TODO: Moderator permission instead of just admin
    [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
    public class PlayerManagement : ELOBase
    {
        [Command("ModifyStates")]
        public async Task ModifyStatesAsync()
        {
            var states = Extensions.ConvertEnumToDictionary<Player.ModifyState>();
            await ReplyAsync(string.Join("\n", states.Select(x => x.Key)));
        }

        //TODO: Response should be an embed.
        [Command("Points")]
        public async Task PointsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            await PointsAsync(state, amount, user);
        }

        [Command("Points")]
        public async Task PointsAsync(Player.ModifyState state, int amount, params SocketGuildUser[] users)
        {
            var players = users.Select(x => Context.Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var newVal = Player.ModifyValue(state, player.Points, amount);
                responseString += $"{player.DisplayName}: {player.Points} => {newVal}\n";
                player.Points = newVal;
            }
            Context.Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
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
            //TODO: Value should be case insensitive when searching
            var players = users.Select(x => Context.Service.GetPlayer(Context.Guild.Id, x.Id)).ToList();
            var responseString = "";
            foreach (var player in players)
            {
                var response = player.UpdateValue(value, state, amount);
                responseString += $"{player.DisplayName}: {response.Item1} => {response.Item2}\n";
            }
            Context.Service.Database.StoreMany(players, x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("", false, responseString.QuickEmbed());
        }
    }
}