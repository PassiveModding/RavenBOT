using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using RavenBOT.Discord.Context.Interactive.Callbacks;
using RavenBOT.Discord.Context.Interactive.Criteria;

namespace RavenBOT.Discord.Context.Interactive.Paginator
{
    public class PaginatedMessageCallback : IReactionCallback
    {
        private readonly PaginatedMessage _pager;
        private readonly int pages;
        private int page = 1;


        public PaginatedMessageCallback(InteractiveService interactive,
            SocketCommandContext sourceContext,
            PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null)
        {
            Interactive = interactive;
            Context = sourceContext;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            _pager = pager;
            pages = _pager.Pages.Count();
        }

        public InteractiveService Interactive { get; }
        public IUserMessage Message { get; private set; }

        private PaginatedAppearanceOptions options => _pager.Options;
        public SocketCommandContext Context { get; }

        public RunMode RunMode => RunMode.Sync;
        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout => options.Timeout;

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(options.First))
            {
                page = 1;
            }
            else if (emote.Equals(options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(options.Last))
            {
                page = pages;
            }
            else if (emote.Equals(options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            else if (emote.Equals(options.Jump))
            {
                _ = Task.Run(async () =>
                {
                    var criteria = new Criteria<SocketMessage>()
                        .AddCriterion(new EnsureSourceChannelCriterion())
                        .AddCriterion(new EnsureFromUserCriterion(reaction.UserId))
                        .AddCriterion(new EnsureIsIntegerCriterion());
                    var response = await Interactive.NextMessageAsync(Context, criteria, TimeSpan.FromSeconds(15));
                    var request = int.Parse(response.Content);
                    if (request < 1 || request > pages)
                    {
                        _ = response.DeleteAsync().ConfigureAwait(false);
                        await Interactive.ReplyAndDeleteAsync(Context, options.Stop.Name);
                        return;
                    }

                    page = request;
                    _ = response.DeleteAsync().ConfigureAwait(false);
                    await RenderAsync().ConfigureAwait(false);
                });
            }
            else if (emote.Equals(options.Info))
            {
                await Interactive.ReplyAndDeleteAsync(Context, options.InformationText, timeout: options.InfoTimeout);
                return false;
            }

            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task DisplayAsync(bool showall = false, bool showindex = false)
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(_pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                if (showall) await message.AddReactionAsync(options.First);

                await message.AddReactionAsync(options.Back);
                await message.AddReactionAsync(options.Next);
                if (showall) await message.AddReactionAsync(options.Last);


                var manageMessages = Context.Channel is IGuildChannel guildChannel &&
                                     (Context.User as IGuildUser).GetPermissions(guildChannel).ManageMessages;

                if (showindex)
                    if (options.JumpDisplayOptions == JumpDisplayOptions.Always
                        || options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages)
                        await message.AddReactionAsync(options.Jump);

                if (showall)
                {
                    await message.AddReactionAsync(options.Stop);

                    if (options.DisplayInformationIcon)
                        await message.AddReactionAsync(options.Info);
                }
            });

            if (Timeout.HasValue)
            {
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(message);
                    Message.DeleteAsync();
                });
            }
        }

        public async Task DisplayAsync(Base.ReactionList Reactions)
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(_pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                if (Reactions.First) await message.AddReactionAsync(options.First);
                if (Reactions.Backward) await message.AddReactionAsync(options.Back);
                if (Reactions.Forward) await message.AddReactionAsync(options.Next);
                if (Reactions.Last) await message.AddReactionAsync(options.Last);


                var manageMessages = Context.Channel is IGuildChannel guildChannel &&
                                     (Context.User as IGuildUser).GetPermissions(guildChannel).ManageMessages;

                if (Reactions.Jump)
                {
                    if (options.JumpDisplayOptions == JumpDisplayOptions.Always || options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages)
                    {
                        await message.AddReactionAsync(options.Jump);
                    }
                }

                if (Reactions.Trash)
                {
                    await message.AddReactionAsync(options.Stop);
                }

                if (Reactions.Info)
                {
                    if (options.DisplayInformationIcon) await message.AddReactionAsync(options.Info);
                }
            });
            
            if (Timeout.HasValue)
            {
                displaytimeout(message, Message);
            }
        }

        public void displaytimeout(RestUserMessage M1, IUserMessage M2)
        {
            if (Timeout.HasValue)
            {
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(M1);
                    M2.DeleteAsync();
                });
            }
        }

        protected Embed BuildEmbed()
        {
            //NOTE: Must have description or wont send?
            var current = _pager.Pages.ElementAt(page - 1);
            var builder = new EmbedBuilder()
                .WithAuthor(_pager.Author)
                .WithColor(_pager.Color)
                .WithDescription(_pager.Pages.ElementAt(page - 1).description)
                .WithImageUrl(current.imageurl ?? _pager.Img)
                .WithUrl(current.titleURL)
                .WithFooter(f => f.Text = string.Format(options.FooterFormat, page, pages))
                .WithTitle(current.dynamictitle ?? _pager.Title);
            foreach (var field in _pager.Pages.ElementAt(page - 1).Fields)
            {
                builder.AddField(field);
            }

            return builder.Build();
        }

        private async Task RenderAsync()
        {
            var embed = BuildEmbed();
            await Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
        }
    }
}