using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;

namespace RavenBOT.Modules
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

        [Command("ping")]
        [Alias("latency")]
        [Summary("Shows the websocket connection's latency and time it takes for me send a message.")]
        public async Task PingAsync()
        {
            // start a new stopwatch to measure the time it takes for us to send a message
            var sw = Stopwatch.StartNew();

            // send the message and store it for later modification
            var msg = await ReplyAsync($"**Websocket latency**: {Context.Client.Latency}ms\n" +
                "**Response**: ...");
            // pause the stopwatch
            sw.Stop();

            // modify the message we sent earlier to display measured time
            await msg.ModifyAsync(x => x.Content = $"**Websocket latency**: {Context.Client.Latency}ms\n" +
                $"**Response**: {sw.Elapsed.TotalMilliseconds}ms");
        }
    }
}