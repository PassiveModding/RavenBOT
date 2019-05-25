using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using RavenBOT.Modules.Media.Methods;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("nsfw.")]
    [RequireNsfw]
    public class NSFW : InteractiveBase<ShardedCommandContext>
    {
        public MediaHelper MediaHelper {get;}
        public NsfwHelper NsfwHelper { get; }
        public Random Random {get;}

        public NSFW()
        {
            Random = new Random();
            MediaHelper = new MediaHelper();
            NsfwHelper = new NsfwHelper(Random);
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

            await ReplyAsync($"{selectedPost.Title}\nhttps://reddit.com{selectedPost.Permalink}");
        }

        [Command("nsfw", RunMode = RunMode.Async)]
        [Summary("Shorthand for redditpost nsfw")]
        public async Task NsfwPost()
        {
            await GetPostAsync("NSFW");
        }

        [Command("NsfwGif", RunMode = RunMode.Async)]        
        [Summary("Shorthand for redditpost with various nsfw gif subreddits")]
        public Task NsfwGifAsync()
        {
            var rnd = new Random();
            var subs = new[] { "nsfwgif", "booty_gifs", "boobgifs", "creampiegifs", "pussyjobs", "gifsgonewild", "nsfw_gif", "nsfw_gifs", "porn_gifs", "adultgifs" };
            return GetPostAsync(subs[rnd.Next(subs.Length - 1)]);
        }

        [Command("pussy", RunMode = RunMode.Async)]        
        [Summary("Shorthand for redditpost with various subreddits focussed on the female genitals")]
        public Task PussyAsync()
        {
            var rnd = new Random();
            var subs = new[] { "grool", "creampies", "creampie", "creampiegifs", "pussyjobs", "pussyslip", "upskirt", "pussy", "rearpussy", "simps", "vagina", "moundofvenus" };
            return GetPostAsync(subs[rnd.Next(subs.Length - 1)]);
        }

        [Command("Rule34", RunMode = RunMode.Async)]
        [Alias("R34")]
        [Summary("Search Rule34 Porn using tags")]
        public async Task R34Async(params string[] tags)
        {
            var result = await NsfwHelper.HentaiAsync(NsfwHelper.NsfwType.Rule34, tags.ToList());
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [R34]", Url = $"http://adult.passivenation.com/18217229/{result}", Footer = new EmbedFooterBuilder { Text = string.Join(", ", tags) } };
                await ReplyAsync(string.Empty, false, embed.Build());
            }
        }

        [Command("Yandere", RunMode = RunMode.Async)]
        [Summary("Search Yandere Porn using tags")]
        public async Task YandereAsync(params string[] tags)
        {
            var result = await NsfwHelper.HentaiAsync(NsfwHelper.NsfwType.Yandere, tags.ToList());
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [Yandere]", Url = $"http://adult.passivenation.com/18217229/{result}", Footer = new EmbedFooterBuilder() };
                embed.Footer.Text = string.Join(", ", tags);
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

            obj = JArray.Parse(await MediaHelper.Client.GetStringAsync($"http://api.oboobs.ru/boobs/{rnd}"))[0];

            var builder = new EmbedBuilder { ImageUrl = $"http://media.oboobs.ru/{obj["preview"]}", Description = $"Tits Database Size: 10229\n Image Number: {rnd}", Title = "Tits", Url = $"http://adult.passivenation.com/18217229/http://media.oboobs.ru/{obj["preview"]}" };

            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("Ass", RunMode = RunMode.Async)]
        [Summary("gets a random image from the obutts api")]
        public async Task BumsAsync()
        {
            JToken obj;
            var rnd = Random.Next(0, 4222);
            obj = JArray.Parse(await MediaHelper.Client.GetStringAsync($"http://api.obutts.ru/butts/{rnd}"))[0];

            var builder = new EmbedBuilder { ImageUrl = $"http://media.obutts.ru/{obj["preview"]}", Description = $"Ass Database Size: 4222\n Image Number: {rnd}", Title = "Ass", Url = $"http://adult.passivenation.com/18217229/http://media.obutts.ru/{obj["preview"]}" };
            await ReplyAsync(string.Empty, false, builder.Build());
        }

        [Command("Cureninja", RunMode = RunMode.Async)]
        [Summary("Search Cureninja Porn using tags")]
        public async Task CureninjaAsync(params string[] tags)
        {
            var result = await NsfwHelper.HentaiAsync(NsfwHelper.NsfwType.Cureninja, tags.ToList());
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [Cureninja]", Url = $"http://adult.passivenation.com/18217229/{result}", Footer = new EmbedFooterBuilder { Text = string.Join(", ", tags) } };
                await ReplyAsync(string.Empty, false, embed.Build());
            }
        }

        [Command("Gelbooru", RunMode = RunMode.Async)]
        [Summary("Search Gelbooru Porn using tags")]
        public async Task GelbooruAsync(params string[] tags)
        {
            var result = await NsfwHelper.HentaiAsync(NsfwHelper.NsfwType.Gelbooru, tags.ToList());
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [Gelbooru]", Url = $"http://adult.passivenation.com/18217229/{result}", Footer = new EmbedFooterBuilder { Text = string.Join(", ", tags) } };
                await ReplyAsync(string.Empty, false, embed.Build());
            }
        }

        [Command("Konachan", RunMode = RunMode.Async)]
        [Summary("Search Konachan Porn using tags")]
        public async Task KonachanAsync(params string[] tags)
        {
            var result = await NsfwHelper.HentaiAsync(NsfwHelper.NsfwType.Konachan, tags.ToList());
            if (result == null)
            {
                await ReplyAsync("No Results.");
            }
            else
            {
                var embed = new EmbedBuilder { ImageUrl = result, Title = "View On Site [Konachan]", Url = $"http://adult.passivenation.com/18217229/{result}", Footer = new EmbedFooterBuilder { Text = string.Join(", ", tags) } };
                await ReplyAsync(string.Empty, false, embed.Build());
            }
        }
    }
}