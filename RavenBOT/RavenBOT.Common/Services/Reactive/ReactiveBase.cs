using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common
{
    public class ReactiveBase : ModuleBase<ShardedCommandContext>
    {
        public ReactiveService ReactiveService { get; set; }

        public async Task<IUserMessage> PagedReplyAsync(ReactivePagerCallback pagerCallback)
        {
            var res = await ReactiveService.SendPagedMessageAsync(Context, pagerCallback);
            return res;
        }

        public async Task<IUserMessage> SimpleEmbedAsync(string content, Color? color = null)
        {
            var embed = new EmbedBuilder();
            embed.Description = content.FixLength(2047);
            embed.Color = color ?? Color.Default;
            return await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}