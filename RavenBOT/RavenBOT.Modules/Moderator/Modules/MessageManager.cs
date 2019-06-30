using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MoreLinq;
using RavenBOT.Common.Attributes;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Moderator.Modules
{
    public partial class Moderation
    {
        public async Task<List<IMessage>> GetmessagesAsync(int count = 100)
        {
            var msgs = await Context.Channel.GetMessagesAsync(count).FlattenAsync();
            return msgs.Where(x => x.Timestamp.UtcDateTime + TimeSpan.FromDays(14) > DateTime.UtcNow).ToList();
        }

        [Command("prune")]
        [Alias("purge", "clear")]
        [Summary("removes specified amount of messages")]
        public async Task Prune(int count = 100)
        {
            if (count < 1)
            {
                await ReplyAsync("**ERROR: **Please Specify the amount of messages you want to clear");
            }
            else if (count > 100)
            {
                await ReplyAsync("**Error: **I can only clear 100 Messages at a time!");
            }
            else
            {
                await Context.Message.DeleteAsync().ConfigureAwait(false);
                var limit = count < 100 ? count : 100;
                //var enumerable = await Context.Channel.GetMessagesAsync(limit).Flatten().ConfigureAwait(false);
                var enumerable = await GetmessagesAsync(limit);
                try
                {
                    await ModHandler.LogMessageAsync(Context, $"Cleared **{enumerable.Count}** Messages in {Context.Channel.Name}", null);
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(enumerable).ConfigureAwait(false);
                }
                catch
                {
                    //
                }

                await ReplyAsync($"Cleared **{enumerable.Count}** Messages");
            }
        }

        [Command("prune")]
        [Alias("purge", "pruneuser", "clear")]
        [Summary("removes messages from a user in the last 100 messages")]
        public async Task Prune(SocketGuildUser user)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            //var enumerable = await Context.Channel.GetMessagesAsync().Flatten().ConfigureAwait(false);
            var enumerable = await GetmessagesAsync();
            var newlist = enumerable.Where(x => x.Author == user).ToList();
            try
            {
                await ModHandler.LogMessageAsync(Context, $"Cleared **{newlist.Count}** Messages in {Context.Channel.Name} for {user.Mention}", user);
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(newlist).ConfigureAwait(false);
            }
            catch
            {
                //
            }

            await ReplyAsync($"Cleared **{user.Username}'s** Messages (Count = {newlist.Count})");
        }

        [Command("pruneID")]
        [Summary("removes messages from a user ID in the last 100 messages")]
        public async Task Prune(ulong userID)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = await GetmessagesAsync();
            var newlist = enumerable.Where(x => x.Author.Id == userID).ToList();
            try
            {
                await ModHandler.LogMessageAsync(Context, $"Cleared **{newlist.Count}** Messages in {Context.Channel.Name} for user with ID: {userID}", userID);
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(newlist).ConfigureAwait(false);
            }
            catch
            {
                //
            }

            await ReplyAsync($"Cleared Messages (Count = {newlist.Count})");
        }

        [Command("prune")]
        [Alias("purge", "prunerole", "clear")]
        [Summary("removes messages from a role in the last 100 messages")]
        public async Task Prune(IRole role)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = await GetmessagesAsync();

            var messages = enumerable.ToList().Where(x =>
            {
                var gUser = Context.Guild.GetUser(x.Author.Id);
                if (gUser == null)
                {
                return false;
                }
                return gUser.Roles.Any(r => r.Id == role.Id);
            }).ToList();

            try
            {
                await ModHandler.LogMessageAsync(Context, $"Cleared **{messages.Count}** Messages in {Context.Channel.Name} for {role.Mention}", null);
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages).ConfigureAwait(false);
            }
            catch
            {
                //
            }

            await ReplyAsync($"Cleared Messages (Count = {messages.Count})");
        }

        [Command("Announce", RunMode = RunMode.Async)]
        [RavenRequireUserPermission(GuildPermission.ManageRoles)]
        [Summary("Enabled role mentions, sends messages and then disables again")]
        public async Task Announce(params IRole[] roles)
        {
            //Ensure the bot is only editing roles which aren't already mentionable and are actually able to be edited (are lower than the bot's heirachal role).
            var unmentionable = roles.Where(x => !x.IsMentionable && x.Position < Context.Guild.CurrentUser.Hierarchy).DistinctBy(x => x.Id).ToList();
            foreach (var role in unmentionable)
            {
                await role.ModifyAsync(x => x.Mentionable = true);
            }

            await ReplyAsync("Roles can now be mentioned until you send your next message.");
            try
            {
                await NextMessageAsync();
            }
            finally
            {
                foreach (var role in unmentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = false);
                }                
            }
        }
    }
}