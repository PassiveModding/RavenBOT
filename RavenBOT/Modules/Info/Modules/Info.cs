using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Info.Modules
{
    [Group("info")]
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        [Command("RoleMembers")]
        [Summary("Displays all members in a specific role")]
        public async Task RoleMembersAsync(SocketRole role)
        {
            await Context.Guild.DownloadUsersAsync();
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = $"{role.Name} Members",
                Description = $"{string.Join(", ", role.Members.Select(x => x.Mention))}".FixLength(2047)
            }.Build());
        }
    }
}