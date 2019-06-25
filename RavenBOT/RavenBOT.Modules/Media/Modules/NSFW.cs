using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Modules.Media.Methods;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("nsfw")]
    [RavenRequireNsfw]
    public class NSFW : InteractiveBase<ShardedCommandContext>
    {
        public MediaHelper MediaHelper { get; }
        public HelpService HelpService { get; }
        public Random Random { get; }
        public GfycatManager GfyManager { get; }

        public NSFW(Random random, GfycatManager gfyManager, MediaHelper mediaHelper, HelpService helpService)
        {
            Random = random;
            GfyManager = gfyManager;
            MediaHelper = mediaHelper;
            HelpService = helpService;
        }

        [Command("RedditPost", RunMode = RunMode.Async)]
        [Summary("Get a random post from first 25 in hot of a sub")]
        public async Task GetPostAsync(string subreddit)
        {
            var sub = await MediaHelper.Reddit.GetSubredditAsync(subreddit);
            if (sub == null)
            {
                await ReplyAsync("A subreddit with that name could not be found.");
                return;
            }

            var posts = await sub.GetPosts(RedditSharp.Things.Subreddit.Sort.Hot, 25).OrderByDescending(x => Random.Next()).ToList();

            var selectedPost = posts.FirstOrDefault();

            if (selectedPost == null)
            {
                await ReplyAsync("Unable to retrieve a post.");
                return;
            }

            await ReplyAsync($"{selectedPost.Title}\nhttps://reddit.com{selectedPost.Permalink}", false, new EmbedBuilder()
            {
                //Note that non gfycat urls will be returned as normal even with this function
                ImageUrl = await GfyManager.GetGfyCatUrl(selectedPost.Url.ToString())
            }.Build());
        }

        [Command(RunMode = RunMode.Async)]
        [Alias("nsfw")]
        [Summary("Shorthand for redditpost nsfw")]
        public async Task NsfwPost()
        {
            await GetPostAsync("NSFW");
        }

        [Command("NsfwGif", RunMode = RunMode.Async)]
        [Alias("gif")]
        [Summary("Shorthand for redditpost with various nsfw gif subreddits")]
        public Task NsfwGifAsync()
        {
            var rnd = new Random();
            var subs = new [] { "nsfwgif", "booty_gifs", "boobgifs", "creampiegifs", "pussyjobs", "gifsgonewild", "nsfw_gif", "nsfw_gifs", "porn_gifs", "adultgifs" };
            return GetPostAsync(subs[rnd.Next(subs.Length - 1)]);
        }

        [Command("pussy", RunMode = RunMode.Async)]
        [Summary("Shorthand for redditpost with various subreddits focussed on the female genitals")]
        public Task PussyAsync()
        {
            var rnd = new Random();
            var subs = new [] { "grool", "creampies", "creampie", "creampiegifs", "pussyjobs", "pussyslip", "upskirt", "pussy", "rearpussy", "simps", "vagina", "moundofvenus" };
            return GetPostAsync(subs[rnd.Next(subs.Length - 1)]);
        }

        [Command("Rule34", RunMode = RunMode.Async)]
        [Alias("R34")]
        [Summary("Search Rule34 Porn using tags")]
        public async Task R34Async(params string[] tags)
        {
            tags = !tags.Any() ? new [] { "boobs", "tits", "ass", "sexy", "neko" } : tags;
            var url = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={string.Join("+", tags.Select(x => x.Replace(" ", "_")))}";

            var get = await MediaHelper.Client.GetStringAsync(url).ConfigureAwait(false);
            var matches = Regex.Matches(get, "file_url=\"(.*?)\" ");
            var result = $"{matches[Random.Next(matches.Count)].Groups[1].Value}";
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [R34]", Url = result, Footer = new EmbedFooterBuilder { Text = string.Join(", ", tags) } };
                await ReplyAsync(string.Empty, false, embed.Build());
            }
        }


        [Command("tits", RunMode = RunMode.Async)]
        [Alias("boobs", "rack")]
        [Summary("gets a random image from the oboobs api")]
        public async Task BoobsAsync()
        {
            JToken obj;
            var rnd = Random.Next(0, 10229);

            obj = JArray.Parse(await MediaHelper.Client.GetStringAsync($"http://api.oboobs.ru/boobs/{rnd}")) [0];

            var builder = new EmbedBuilder { ImageUrl = $"http://media.oboobs.ru/{obj["preview"]}", Description = $"Tits Database Size: 10229\n Image Number: {rnd}", Title = "Tits", Url = $"http://media.oboobs.ru/{obj["preview"]}" };

            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("Ass", RunMode = RunMode.Async)]
        [Summary("gets a random image from the obutts api")]
        public async Task BumsAsync()
        {
            JToken obj;
            var rnd = Random.Next(0, 4222);
            obj = JArray.Parse(await MediaHelper.Client.GetStringAsync($"http://api.obutts.ru/butts/{rnd}")) [0];

            var builder = new EmbedBuilder { ImageUrl = $"http://media.obutts.ru/{obj["preview"]}", Description = $"Ass Database Size: 4222\n Image Number: {rnd}", Title = "Ass", Url = $"http://media.obutts.ru/{obj["preview"]}" };
            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "nsfw"
            });

            if (res != null)
            {
                await PagedReplyAsync(res, new ReactionList
                {
                    Backward = true,
                        First = false,
                        Forward = true,
                        Info = false,
                        Jump = true,
                        Last = false,
                        Trash = true
                });
            }
            else
            {
                await ReplyAsync("N/A");
            }
        }
    }
}