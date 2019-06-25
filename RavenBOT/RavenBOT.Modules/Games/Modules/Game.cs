using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Modules.Games.Methods;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games")]
    public partial class Game : InteractiveBase<ShardedCommandContext>
    {
        public GameService GameService { get; }
        public HelpService HelpService { get; }
        public Random Random { get; }
        public HttpClient HttpClient { get; }

        public Game(GameService gameService, HelpService helpService, Random random, HttpClient client)
        {
            GameService = gameService;
            HelpService = helpService;
            Random = random;
            HttpClient = client;
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "games"
            }, "This module contains fun games to play");

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

        [Command("DailyReward", RunMode = RunMode.Async)]
        [Summary("Get 200 free coins")]
        [RateLimit(2, 23, Measure.Hours)]        
        [Remarks("Limited to 2 uses daily")]
        public async Task DailyRewardAsync()
        {
            var guildobj = GameService.GetGameServer(Context.Guild.Id);
            var guser = GameService.GetGameUser(Context.User.Id, Context.Guild.Id);
            guser.Points = guser.Points + 200;
            GameService.SaveGameUser(guser);
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
        [Summary("Get a user's game stats")]
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