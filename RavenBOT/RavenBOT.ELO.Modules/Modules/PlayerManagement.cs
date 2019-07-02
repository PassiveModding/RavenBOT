using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    //TODO: Moderator permission instead of just admin
    //TODO: Use raven preconditions instead of discord defaults
    [RequireUserPermission(Discord.GuildPermission.Administrator)]
    public class PlayerManagement : ELOBase
    {

        [Command("Points")]
        public async Task PointsAsync(SocketGuildUser user, Player.ModifyState state, int amount)
        {
            var player = Context.Service.GetPlayer(Context.Guild.Id, Context.User.Id);
            player.Points = Player.ModifyValue(state, player.Points, amount);
            Context.Service.Database.Store(player, Player.DocumentName(player.GuildId, player.UserId));
            //TODO: accept multiple users to be modified
            //TODO: Reply with modified amount/player info(s)
            await ReplyAsync("Points set.");
        }

        [Command("PlayerModify")]
        public async Task PlayerModifyAsync(SocketGuildUser user, string value, Player.ModifyState state, int amount)
        {
            //TODO: case insensitive value.
            var player = Context.Service.GetPlayer(Context.Guild.Id, Context.User.Id);
            player.UpdateValue(value, state, amount);
            Context.Service.Database.Store(player, Player.DocumentName(player.GuildId, player.UserId));
            //TODO: accept multiple users to be modified
            //TODO: Reply with modified amount/player info(s)
            await ReplyAsync("Points set.");
        }
    }
}