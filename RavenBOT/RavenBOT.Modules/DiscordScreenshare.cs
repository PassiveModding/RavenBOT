using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;

namespace RavenBOT.Modules
{
    public class DiscordScreenshare : InteractiveBase<ShardedCommandContext>
    {
        [Command("ScreenShare")]
        [Summary("Allows you to create a screenshare directly within your discord server rather than a group chat.")]
        [RavenRequireContext(ContextType.Guild)]
        public async Task MakeScreenshare()
        {
            if (!(Context.User is SocketGuildUser user))
            {
                return;
            }

            if (user.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voice channel for this to run.");
                return;
            }

            var link = $"Click this link **while** in the voice channel and you will be placed in a video channel.\n" +
                $"https://discordapp.com/channels/{Context.Guild.Id}/{user.VoiceChannel.Id}\n" +
                $"Anyone who wishes to view the screenshare must also first join the audio channel and then click the link.";
            await ReplyAsync("", false, link.QuickEmbed());
        }
    }
}