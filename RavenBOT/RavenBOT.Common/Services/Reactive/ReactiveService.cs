using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace RavenBOT.Common
{
    public class ReactiveService : IDisposable, IServiceable
    {
        public DiscordShardedClient Client { get; }
        
        private readonly Dictionary<ulong, IReactiveCallback> callbacks;
        public ReactiveService(DiscordShardedClient client)
        {
            Client = client;
            callbacks = new Dictionary<ulong, IReactiveCallback>();
            Client.ReactionAdded += HandleReactionAsync;
        }

        public void AddReactionCallback(IMessage message, IReactiveCallback callback)
            => callbacks[message.Id] = callback;

        public async Task<IUserMessage> SendPagedMessageAsync(ShardedCommandContext context, ReactivePagerCallback pagerCallback)
        {
            await pagerCallback.DisplayAsync(context, this);
            callbacks.Add(pagerCallback.Message.Id, pagerCallback);
            return pagerCallback.Message;
        }

        public async Task<IUserMessage> ReplyAndDeleteAsync(ShardedCommandContext context, string content, Embed embed, TimeSpan? timeout = null)
        {
            var message = await ReplyAsync(context, content, embed);
            _ = Task.Delay(timeout ?? TimeSpan.FromSeconds(15))
                .ContinueWith(_ => message.DeleteAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
                return message;
        }

        public async Task<IUserMessage> ReplyAsync(ShardedCommandContext context, string message, Embed embed)
        {
            return await context.Channel.SendMessageAsync(message, false, embed);
        }

        public async Task<IUserMessage> SimpleEmbedAsync(ShardedCommandContext context, string content, Color? color = null)
        {
            var embed = new EmbedBuilder();
            embed.Description = content.FixLength(2047);
            embed.Color = color ?? Color.Default;
            return await context.Channel.SendMessageAsync("", false, embed.Build());
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
            {
                //Ignore reactions added by the bot itself.
                return;
            }

            if (!callbacks.TryGetValue(message.Id, out var callback))
            {
                //Ensure the message being reacted on is actually one created for the service.
                return;
            }

            switch (callback.RunMode)
            {
                case RunMode.Async:
                    _ = Task.Run(async () =>
                    {
                        if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                        {
                            callbacks.Remove(message.Id);
                        }
                    });
                    break;
                default:
                    if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                    {
                        callbacks.Remove(message.Id);
                    }

                    break;
            }
        }

        public void Dispose()
        {
            Client.ReactionAdded -= HandleReactionAsync;
        }
    }
}