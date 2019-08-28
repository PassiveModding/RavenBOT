using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace RavenBOT.Common.Reactive
{
    public class ReactivePagerCallback : IReactionCallback
    {
        public ReactivePagerCallback(SocketCommandContext context, ReactivePager pager, TimeSpan timeout)
        {
            _timeout = timeout;
            Pager = pager;
            Callbacks = new Dictionary<IEmote, Func<ReactivePagerCallback, SocketReaction, Task<bool>>>();
            _context = context;
            pages = Pager.Pages.Count();            
        }
        public RunMode RunMode => RunMode.Async;

        private TimeSpan _timeout;

        public TimeSpan? Timeout => _timeout;

        /// <summary>
        /// The context of the command when it was initially run.
        /// </summary>
        private SocketCommandContext _context;

        /// <summary>
        /// Refers to the pager being displayed to the user.
        /// Initialized by the 'DisplayAsync' method
        /// </summary>
        public IUserMessage Message;

        /// <summary>
        /// Refers to the context of the command that originally generated the Pager,
        /// NOT the pager itself.
        /// </summary>
        public SocketCommandContext Context => _context;

        public ReactivePager Pager;
        private readonly int pages;
        private int page = 1;

        public Dictionary<IEmote, Func<ReactivePagerCallback, SocketReaction, Task<bool>>> Callbacks { get; set; }

        /// <summary>
        /// This method is called whenever the reactive service detects a reaction on 'Message'
        /// </summary>
        /// <param name="reaction"></param>
        /// <returns>
        /// True if the message is to be unsubscribed from the Service.
        /// </returns>
        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            if (!Pager.Pages.Any())
            {
                return true;
            }

            if (!Callbacks.Any())
            {
                return true;
            }

            if (!Callbacks.TryGetValue(reaction.Emote, out var callback))
            {
                return false;
            }

            return await callback.Invoke(this, reaction).ConfigureAwait(false);
        }

        public async Task<bool> NextAsync(SocketReaction reaction)
        {
            if (page >= pages)
                return false;
            ++page;
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task<bool> PreviousAsync(SocketReaction reaction)
        {
            if (page <= 1)
                return false;
            --page;
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task<bool> FirstAsync(SocketReaction reaction)
        {
            page = 1;
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task<bool> LastAsync(SocketReaction reaction)
        {
            page = pages;
            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task<bool> TrashAsync(SocketReaction reaction)
        {
            await Message.DeleteAsync().ConfigureAwait(false);
            return true;
        }

        public Task RenderAsync()
        {
            var embed = BuildEmbed();
            return Message.ModifyAsync(m => m.Embed = embed);
        }

        protected Embed BuildEmbed()
        {
            ReactivePage current = null;
            if (Pager.Pages.Any())
            {
                current = Pager.Pages.ElementAt(page - 1);
            }

            var builder = new EmbedBuilder
            {
                Author = current?.Author ?? Pager.Author,
                Title = current?.Title ?? Pager.Title,
                Url = current?.Url ?? Pager.Url,
                Description = current?.Description ?? Pager.Description,
                ImageUrl = current?.ImageUrl ?? Pager.ImageUrl,
                Color = current?.Color ?? Pager.Color,
                Fields = current?.Fields ?? Pager.Fields,
                Footer = current?.FooterOverride ?? Pager.FooterOverride ?? new EmbedFooterBuilder
                {
                    Text = $"{page}/{pages}"
                },
                ThumbnailUrl = current?.ThumbnailUrl ?? Pager.ThumbnailUrl,
                Timestamp = current?.TimeStamp ?? Pager.TimeStamp
            };

            return builder.Build();
        }

        /// <summary>
        /// Sends the initial pager message and sets the 'Message' to it's response.
        /// </summary>
        /// <returns></returns>
        public async Task DisplayAsync()
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(Pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            if (Callbacks.Any())
            {
                await Message.AddReactionsAsync(Callbacks.Select(x => x.Key).ToArray());
            }
        }

        /// <summary>
        /// Builder for Reactive pager.
        /// </summary>
        /// <param name="emote"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ReactivePagerCallback WithCallback(IEmote emote, Func<ReactivePagerCallback, SocketReaction, Task<bool>> callback)
        {
            Callbacks.Add(emote, callback);
            return this;
        }
    }
}