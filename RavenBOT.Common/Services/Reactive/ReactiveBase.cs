using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

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

        public Task<Dictionary<ulong, IUserMessage>> MessageUsersAsync(ulong[] userIds, string content, Embed embed = null)
                            => ReactiveService.MessageUsersAsync(Context, userIds, content, embed);

        public Task<Dictionary<ulong, IUserMessage>> MessageUsersAsync(ulong[] userIds, Func<ulong, string> contentFunc, Embed embed = null)
                            => ReactiveService.MessageUsersAsync(Context, userIds, contentFunc, embed);

        public Task<Dictionary<ulong, IUserMessage>> MessageUsersAsync(ulong[] userIds, Func<ulong, string> contentFunc, Func<ulong, Embed> embed = null)
                            => ReactiveService.MessageUsersAsync(Context, userIds, contentFunc, embed);

        public Task<SocketMessage> NextMessageAsync(Func<SocketCommandContext, SocketMessage, Task<bool>> judge, TimeSpan? timeout = null)
                            => ReactiveService.NextMessageAsync(Context, judge, timeout);
    }
}