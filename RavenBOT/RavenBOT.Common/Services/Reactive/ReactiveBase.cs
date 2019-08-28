using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common
{
    public class ReactiveBase : ModuleBase<ShardedCommandContext>
    {
        public ReactiveService ReactiveService { get; set; }

        public Task<IUserMessage> PagedReplyAsync(ReactivePagerCallback pagerCallback)
                            => ReactiveService.SendPagedMessageAsync(Context, pagerCallback);

        public Task<IUserMessage> SimpleEmbedAsync(string content, Color? color = null) 
                            => ReactiveService.SimpleEmbedAsync(Context, content, color);

        public Task<IUserMessage> ReplyAndDeleteAsync(string content, Embed embed = null, TimeSpan? timeout = null)
                            => ReactiveService.ReplyAndDeleteAsync(Context, content, embed, timeout);
    }
}