namespace RavenBOT.Discord.Context
{
    using System;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    /// <summary>
    /// The module base.
    /// </summary>
    public abstract class Base : ModuleBase<Context>
    {
        /// <summary>
        /// Gets or sets Our Custom Interactive Base
        /// </summary>
        public Interactive Interactive { get; set; }

        /// <summary>
        /// Reply in the server. Shorthand for Context.Channel.SendMessageAsync()
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="embed">
        /// The embed.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<IUserMessage> ReplyAsync(string message, Embed embed = null)
        {
            await Context.Channel.TriggerTypingAsync();
            return await ReplyAsync(message, false, embed);
        }

        /// <summary>
        /// Shorthand for  replying with just an embed
        /// </summary>
        /// <param name="embed">
        /// The embed.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
        {
            return await ReplyAsync(string.Empty, false, embed.Build());
        }

        /// <summary>
        /// Shorthand for  replying with just an embed
        /// </summary>
        /// <param name="embed">
        /// The embed.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<IUserMessage> ReplyAsync(Embed embed)
        {
            return await ReplyAsync(string.Empty, false, embed);
        }

        /// <summary>
        /// Reply in the server and then delete after the provided delay.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<IUserMessage> ReplyAndDeleteAsync(string message, TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(5);
            var msg = await Context.Channel.SendMessageAsync(message).ConfigureAwait(false);
            _ = Task.Delay(timeout.Value).ContinueWith(_ => msg.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            return msg;
        }

        /// <summary>
        ///     Just shorthand for saving our guild config
        /// </summary>
        public void Save()
        {
            Context.Server.Save();
        }

        /// <summary>
        ///     Rather than just replying, we can spice things up a bit and embed them in a small message
        /// </summary>
        /// <param name="message">The text that will be contained in the embed</param>
        /// <returns>The message that was sent</returns>
        public async Task<IUserMessage> SimpleEmbedAsync(string message)
        {
            var embed = new EmbedBuilder
            {
                Description = message,
                Color = Color.DarkOrange
            };
            return await ReplyAsync(string.Empty, false, embed.Build());
        }

        /// <summary>
        ///     This will generate a paginated message which allows users to use reactions to change the content of the message
        /// </summary>
        /// <param name="pager">Our paginated message</param>
        /// <param name="reactionList">The reaction config</param>
        /// <param name="fromSourceUser">True = only Context.User may react to the message</param>
        /// <returns>The message that was sent</returns>
        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ReactionList reactionList, bool fromSourceUser = true)
        {
            var criterion = new Criteria<SocketReaction>();
            if (fromSourceUser)
            {
                criterion.AddCriterion(new EnsureReactionFromSourceUserCriterion());
            }

            return PagedReplyAsync(pager, criterion, reactionList);
        }

        /// <summary>
        /// Sends a paginated message
        /// </summary>
        /// <param name="pager">The paginated message</param>
        /// <param name="criterion">The criterion for the reply</param>
        /// <param name="reactions">Customized reaction list</param>
        /// <returns>The message sent.</returns>
        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ICriterion<SocketReaction> criterion, ReactionList reactions)
        {
            return Interactive.SendPaginatedMessageAsync(SocketContext(), pager, reactions, criterion);
        }

        /// <summary>
        ///     Waits for the next message. NOTE: Your run-mode must be async or this will lock up.
        /// </summary>
        /// <param name="criterion">The criterion for the message</param>
        /// <param name="timeout">Time to wait before exiting</param>
        /// <returns>The message received</returns>
        public Task<SocketMessage> NextMessageAsync(ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(SocketContext(), criterion, timeout);
        }

        /// <summary>
        /// Waits until a new message is sent in the channel.
        /// </summary>
        /// <param name="fromSourceUser">Command invoker only</param>
        /// <param name="inSourceChannel">Context.Channel only</param>
        /// <param name="timeout">Time before exiting</param>
        /// <returns>The message received</returns>
        public Task<SocketMessage> NextMessageAsync(bool fromSourceUser = true, bool inSourceChannel = true, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(SocketContext(), fromSourceUser, inSourceChannel, timeout);
        }

        /// <summary>
        ///     Sends a message that will do a custom action upon reactions
        /// </summary>
        /// <param name="data">The main settings used for the message</param>
        /// <param name="fromSourceUser">True = Only the user who invoked this method can invoke the callback</param>
        /// <returns>The message sent</returns>
        public Task<IUserMessage> InlineReactionReplyAsync(ReactionCallbackData data, bool fromSourceUser = true)
        {
            return Interactive.SendMessageWithReactionCallbacksAsync(SocketContext(), data, fromSourceUser);
        }

        /// <summary>
        ///     Send a message that self destructs after a certain period of time
        /// </summary>
        /// <param name="content">The text of the message being sent</param>
        /// <param name="embed">A build embed to be sent</param>
        /// <param name="timeout">The time it takes before the message is deleted</param>
        /// <param name="options">Request Options</param>
        /// <returns>The message that was sent</returns>
        public Task<IUserMessage> ReplyAndDeleteAsync(string content, Embed embed = null, TimeSpan? timeout = null, RequestOptions options = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return Interactive.ReplyAndDeleteAsync(SocketContext(), content, false, embed, timeout, options);
        }
        
        /// <summary>
        ///     This is just a shorthand conversion from out custom context to a socket context, for use in things like Interactive
        /// </summary>
        /// <returns>A new SocketCommandContext</returns>
        private SocketCommandContext SocketContext()
        {
            return new SocketCommandContext(Context.Client.GetShardFor(Context.Guild), Context.Message);
        }
    }
}