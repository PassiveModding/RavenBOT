using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Modules.Moderator.Modules 
{
    public partial class Moderation {
        public List<IMessage> GetmessagesAsync (int count = 100) {
            var msgs = Context.Channel.GetMessagesAsync (count).Flatten ();
            return msgs.Where (x => x.Timestamp.UtcDateTime + TimeSpan.FromDays (14) > DateTime.UtcNow).ToList ().Result;
        }

        [Command ("prune")]
        [Alias ("purge", "clear")]
        [Summary ("Mod Prune <no. of messages>")]
        [Remarks ("removes specified amount of messages")]
        public async Task Prune (int count = 100) 
        {
            if (count < 1) 
            {
                await ReplyAsync ("**ERROR: **Please Specify the amount of messages you want to clear");
            } else if (count > 100) 
            {
                await ReplyAsync ("**Error: **I can only clear 100 Messages at a time!");
            } else 
            {
                await Context.Message.DeleteAsync ().ConfigureAwait (false);
                var limit = count < 100 ? count : 100;
                //var enumerable = await Context.Channel.GetMessagesAsync(limit).Flatten().ConfigureAwait(false);
                var enumerable = GetmessagesAsync (limit);
                try 
                {
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync (enumerable).ConfigureAwait (false);
                } catch 
                {
                    //
                }

                await ReplyAsync ($"Cleared **{enumerable.Count}** Messages");
            }
        }

        [Command ("prune")]
        [Alias ("purge", "pruneuser", "clear")]
        [Summary ("Mod Prune <user>")]
        [Remarks ("removes messages from a user in the last 100 messages")]
        public async Task Prune (IUser user) 
        {
            await Context.Message.DeleteAsync ().ConfigureAwait (false);
            //var enumerable = await Context.Channel.GetMessagesAsync().Flatten().ConfigureAwait(false);
            var enumerable = GetmessagesAsync ();
            var newlist = enumerable.Where (x => x.Author == user).ToList ();
            try 
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync (newlist).ConfigureAwait (false);
            } catch 
            {
                //
            }

            await ReplyAsync ($"Cleared **{user.Username}'s** Messages (Count = {newlist.Count})");
        }

        [Command ("pruneID")]
        [Summary ("pruneID <userID>")]
        [Remarks ("removes messages from a user ID in the last 100 messages")]
        public async Task Prune (ulong userID) 
        {
            await Context.Message.DeleteAsync ().ConfigureAwait (false);
            var enumerable = GetmessagesAsync ();
            var newlist = enumerable.Where (x => x.Author.Id == userID).ToList ();
            try 
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync (newlist).ConfigureAwait (false);
            } catch 
            {
                //
            }

            await ReplyAsync ($"Cleared Messages (Count = {newlist.Count})");
        }

        [Command ("prune")]
        [Alias ("purge", "prunerole", "clear")]
        [Summary ("Prune <@role>")]
        [Remarks ("removes messages from a role in the last 100 messages")]
        public async Task Prune (IRole role) 
        {
            await Context.Message.DeleteAsync ().ConfigureAwait (false);
            var enumerable = GetmessagesAsync ();

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
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages).ConfigureAwait (false);
            } catch 
            {
                //
            }

            await ReplyAsync ($"Cleared Messages (Count = {messages.Count})");
        }
    }
}