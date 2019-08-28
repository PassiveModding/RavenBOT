using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common.Reactive
{
    public class ReactiveBase : ModuleBase<ShardedCommandContext>
    {
        public ReactiveService Service { get; set; }

        public Task<IUserMessage> PagedReplyAsync(ReactivePagerCallback pagerCallback)
                => Service.SendPagedMessageAsync(Context, pagerCallback);

        public async Task<IUserMessage> SimpleEmbedAsync(string content, Color? color = null)
        {
            var embed = new EmbedBuilder();
            embed.Description = content.FixLength(2047);
            embed.Color = color ?? Color.Default;
            return await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}