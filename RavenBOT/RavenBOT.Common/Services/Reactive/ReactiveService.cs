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
            await pagerCallback.DisplayAsync(context);
            callbacks.Add(pagerCallback.Message.Id, pagerCallback);
            return pagerCallback.Message;
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