using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Handlers;
using RavenBOT.Models;

namespace RavenBOT.Discord.Context
{
    public abstract class Base : ModuleBase<Context>
    {
        public Interactive Interactive { get; set; }

        /// <summary>
        ///     Reply in the server. This is a shortcut for context.channel.sendmessageasync
        /// </summary>
        public async Task<IUserMessage> ReplyAsync(string Message, Embed Embed = null)
        {
            await Context.Channel.TriggerTypingAsync();
            return await ReplyAsync(Message, false, Embed);
        }

        /// <summary>
        ///     Shorthand for  replying with just an embed
        /// </summary>
        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
        {
            return await ReplyAsync("", false, embed.Build());
        }

        public async Task<IUserMessage> ReplyAsync(Embed embed)
        {
            return await ReplyAsync("", false, embed);
        }


        /// <summary>
        ///     Reply in the server and then delete after the provided delay.
        /// </summary>
        public async Task<IUserMessage> ReplyAndDeleteAsync(string Message, TimeSpan? Timeout = null)
        {
            Timeout = Timeout ?? TimeSpan.FromSeconds(5);
            var Msg = await Context.Channel.SendMessageAsync(Message).ConfigureAwait(false);
            _ = Task.Delay(Timeout.Value).ContinueWith(_ => Msg.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            return Msg;
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
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<IUserMessage> SimpleEmbedAsync(string message)
        {
            var embed = new EmbedBuilder
            {
                Description = message,
                Color = Color.DarkOrange
            };
            return await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     This is just a shorthand conversion from out custom context to a socketcontext, for use in things like Interactive
        /// </summary>
        /// <returns></returns>
        private SocketCommandContext SocketContext()
        {
            return new SocketCommandContext(Context.Client.GetShardFor(Context.Guild), Context.Message);
        }

        /// <summary>
        ///     This will gnerate a paginated message which allows users to use reactions to change the content of the message
        /// </summary>
        /// <param name="pager"></param>
        /// <param name="Reactions">The reaction config</param>
        /// <param name="fromSourceUser">True = only Context.User may react to the message</param>
        /// <returns></returns>
        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ReactionList Reactions, bool fromSourceUser = true)
        {
            var criterion = new Criteria<SocketReaction>();
            if (fromSourceUser)
            {
                criterion.AddCriterion(new EnsureReactionFromSourceUserCriterion());
            }

            return PagedReplyAsync(pager, criterion, Reactions);
        }

        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ICriterion<SocketReaction> criterion, ReactionList Reactions)
        {
            return Interactive.SendPaginatedMessageAsync(SocketContext(), pager, Reactions, criterion);
        }

        /// <summary>
        ///     Waits for the next message. NOTE: Your runmode must be async or this will lock up.
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<SocketMessage> NextMessageAsync(ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(SocketContext(), criterion, timeout);
        }

        public Task<SocketMessage> NextMessageAsync(bool fromSourceUser = true, bool inSourceChannel = true,
            TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(SocketContext(), fromSourceUser, inSourceChannel, timeout);
        }

        /// <summary>
        ///     Sends a message that will do a custom action upon reactions
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fromSourceUser"></param>
        /// <returns></returns>
        public Task<IUserMessage> InlineReactionReplyAsync(ReactionCallbackData data, bool fromSourceUser = true)
        {
            return Interactive.SendMessageWithReactionCallbacksAsync(SocketContext(), data, fromSourceUser);
        }

        /// <summary>
        ///     Send a message that self destructs after a certain period of time
        /// </summary>
        /// <param name="content"></param>
        /// <param name="embed"></param>
        /// <param name="timeout"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Task<IUserMessage> ReplyAndDeleteAsync(string content, Embed embed = null,
            TimeSpan? timeout = null, RequestOptions options = null)
        {
            return Interactive.ReplyAndDeleteAsync(SocketContext(), content, false, embed, timeout, options);
        }
    }

    public class Context : ShardedCommandContext
    {
        public Context(DiscordShardedClient ClientParam, SocketUserMessage MessageParam, IServiceProvider ServiceProvider) : base(ClientParam, MessageParam)
        {
            //These are our custom additions to the context, giving access to the server object and all server objects through Context.
            Server = ServiceProvider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, Guild.Id);
            Provider = ServiceProvider;
        }

        public GuildModel Server { get; }
        public IServiceProvider Provider { get; }
    }
}