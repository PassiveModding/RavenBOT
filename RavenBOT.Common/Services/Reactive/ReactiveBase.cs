using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RavenBOT.Common
{
    public class ReactiveBase : ModuleBase<ShardedCommandContext>
    {
        public ReactiveService ReactiveService { get; set; }

        public Task<IUserMessage> PagedReplyAsync(ReactivePagerCallback pagerCallback)
                            => ReactiveService.SendPagedMessageAsync(Context, pagerCallback);

        public Task<IUserMessage> SimpleEmbedAsync(string content, Color? color = null)
                            => ReactiveService.SimpleEmbedAsync(Context, content, color);

        public Task<IUserMessage> SimpleEmbedAndDeleteAsync(string content, Color? color = null, TimeSpan? timeout = null)
                    => ReactiveService.SimpleEmbedAndDeleteAsync(Context, content, color, timeout);

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

        public async Task<IUserMessage> ReplyAsync(string message, Embed embed)
        {
            return await Context.Channel.SendMessageAsync(message, false, embed);
        }

        public async Task<IUserMessage> ReplyAsync(Embed embed)
        {
            return await Context.Channel.SendMessageAsync("", false, embed);
        }

        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
        {
            return await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}