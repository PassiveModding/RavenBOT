using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Modules.Games.Methods;
using RavenBOT.Preconditions;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games.")]
        public partial class Game : InteractiveBase<ShardedCommandContext>
    {
        public GameService GameService {get;}
        public Random Random {get;}
        public HttpClient HttpClient {get;}

        public Game(IDatabase database)
        {
            GameService = new GameService(database);
            Random = new Random();
            HttpClient = new HttpClient();
        }

        [Command("DailyReward", RunMode = RunMode.Async)]
        [Summary("DailyReward")]
        [Remarks("Get 200 free coins")]
        [RateLimit(1, 1, Measure.Days)]
        public async Task DailyRewardAsync()
        {
            var guildobj = GameService.GetGameServer(Context.Guild.Id);
            var guser = GameService.GetGameUser(Context.User.Id, Context.Guild.Id);
            guser.Points = guser.Points + 200;

            var embed = new EmbedBuilder
            {
                Title = $"Success, you have received 200 Points",
                Description = $"Balance: {guser.Points} Points",
                ThumbnailUrl = Context.User.GetAvatarUrl(),
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{Context.User.Username}#{Context.User.Discriminator}"
                }
            };

            await ReplyAsync("", false, embed.Build());
        }

        [Command("GameStats", RunMode = RunMode.Async)]
        [Summary("GameStats")]
        [Remarks("Get a user's game stats")]
        public async Task GambleStatsAsync(IUser user = null)
        {
            var guildobj = GameService.GetGameServer(Context.Guild.Id);
            if (user == null)
            {
                user = Context.User;
            }

            var guser = GameService.GetGameUser(user.Id, Context.Guild.Id);

            var embed = new EmbedBuilder
            {
                Title = $"{user.Username} Game Stats",
                Description = $"Balance: {guser.Points} Points\n" +
                              $"Total Bet: {guser.TotalBet} Points\n" +
                              $"Total Paid Out: {guser.TotalWon} Points\n",
                ThumbnailUrl = user.GetAvatarUrl(),
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{user.Username}#{user.Discriminator}"
                }
            };

            await ReplyAsync("", false, embed.Build());
        }
    }
}