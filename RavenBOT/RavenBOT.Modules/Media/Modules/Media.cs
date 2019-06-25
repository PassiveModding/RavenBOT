using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json;
using RavenBOT.Common.Attributes;
using RavenBOT.Extensions;
using RavenBOT.Modules.Media.Methods;
using RavenBOT.Modules.Media.Models;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("Media")]
    public class Media : InteractiveBase<ShardedCommandContext>
    {
        public MediaHelper MediaHelper { get; }
        public Random Random { get; }

        public Media(Random random, MediaHelper mediaHelper)
        {
            Random = random;
            MediaHelper = mediaHelper;
        }

        [Command("RedditPost", RunMode = RunMode.Async)]
        [Summary("Get a random post from first 25 in hot of a sub")]
        public async Task GetPostAsync(string subreddit)
        {
            //NOTE: NSFW Posts/subs are filtered out from this command.
            var sub = await MediaHelper.Reddit.GetSubredditAsync(subreddit);
            if (sub == null)
            {
                await ReplyAsync("A subreddit with that name could not be found.");
                return;
            }

            var posts = await sub.GetPosts(RedditSharp.Things.Subreddit.Sort.Hot, 25).OrderByDescending(x => Random.Next()).ToList();

            RedditSharp.Things.Post selectedPost = null;
            //Filter out nsfw posts
            foreach (var post in posts)
            {
                if (post.NSFW)
                {
                    continue;
                }

                selectedPost = post;
            }

            if (selectedPost == null)
            {
                await ReplyAsync("Unable to retrieve a post.");
                return;
            }

            await ReplyAsync($"{selectedPost.Title}\nhttps://reddit.com{selectedPost.Permalink}");
        }

        [Command("dog")]
        [Summary("Gets a random dog image from random.dog")]
        public async Task DogAsync()
        {
            var woof = "http://random.dog/" + await MediaHelper.Client.GetStringAsync("https://random.dog/woof").ConfigureAwait(false);
            var embed = new EmbedBuilder().WithImageUrl(woof).WithTitle("Woof").WithUrl(woof);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("UrbanDictionary")]
        [RavenRequireNsfw]
        [Summary("Search Urban Dictionary for the specified term")]
        public async Task UrbanAsync([Remainder] string word)
        {
            var res = await MediaHelper.Client.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={word}").ConfigureAwait(false);
            var model = JsonConvert.DeserializeObject<UrbanDictionaryModel>(res);
            if (model.result_type == "no_results")
            {
                await ReplyAsync("This word has no definition");
                return;
            }

            var mostVoted = model.list.OrderByDescending(x => x.thumbs_up).First();
            var emb = new EmbedBuilder { Title = mostVoted.word, Color = Color.LightOrange }.AddField("Definition", $"{mostVoted.definition}", true).AddField("Example", $"{mostVoted.example.FixLength()}", true).AddField("Votes", $"^ [{mostVoted.thumbs_up}] v [{mostVoted.thumbs_down}]");
            await ReplyAsync("", false, emb.Build());
        }

        [Command("xkcd", RunMode = RunMode.Async)]
        [Summary("Get a random xkcd post, or the specified post numer")]
        public async Task XkcdAsync([Summary("the post number, use 'latest' for most recent or leave empty for random")] string number = null)
        {
            string res;
            if (number == "latest")
            {
                res = await MediaHelper.Client.GetStringAsync("https://xkcd.com/info.0.json").ConfigureAwait(false);
            }
            else if (int.TryParse(number, out var result))
            {
                res = await MediaHelper.Client.GetStringAsync($"https://xkcd.com/{result}/info.0.json").ConfigureAwait(false);
            }
            else
            {
                res = await MediaHelper.Client.GetStringAsync($"https://xkcd.com/{Random.Next(1, 2154)}/info.0.json").ConfigureAwait(false);
            }

            var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
            var embed = new EmbedBuilder().WithColor(Color.Blue).WithImageUrl(comic.ImageLink).WithTitle($"{comic.Title}").WithUrl($"https://xkcd.com/{comic.Num}").AddField("Comic Number", $"#{comic.Num}", true).AddField("Date", $"{comic.Month}/{comic.Year}", true);
            var sent = await ReplyAsync("", false, embed.Build());

            await Task.Delay(10000).ConfigureAwait(false);

            await sent.ModifyAsync(m => m.Embed = embed.AddField(efb => efb.WithName("Alt").WithValue(comic.Alt.ToString()).WithIsInline(false)).Build());
        }
    }
}