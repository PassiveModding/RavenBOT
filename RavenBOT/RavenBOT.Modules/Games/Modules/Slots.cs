using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Modules.Games.Modules
{
    public partial class Game
    {
        private readonly List<string> itemList = new List<string>
        {
            "💯", //:100:
            "🌻", //:sunflower:
            "🌑", //:new_moon:
            "🐠", //:tropical_fish: 
            "🎄", //:christmas_tree: 
            "👾", // space invaders
            "⚽" // soccer ball
        };

        [Command("Slots", RunMode = RunMode.Async)]
        [Summary("Play Slots")]
        public async Task Slots(int bet = 0)
        {
            var guildobj = GameService.GetGameServer(Context.Guild.Id);

            // Initially we check whether or not the user is able to bet
            if (bet <= 0)
            {
                await ReplyAsync($"Please place a bet, ie. 10 points!");
                return;
            }

            var guser = GameService.GetGameUser(Context.User.Id, Context.Guild.Id);
            if (bet > guser.Points)
            {
                await ReplyAsync($"Your bet is too high, please place a bet less than or equal to {guser.Points}");
                return;
            }

            // now we deduct the bet amount from the users balance
            guser.Points = guser.Points - bet;

            var selections = new string[3];
            for (var i = 0; i < selections.Length; i++)
            {
                selections[i] = itemList[new Random().Next(0, itemList.Count)];
            }

            // Winning Combos
            // Three of any
            // 3, 2 or 1, :100:'s
            // 3 XMAS Trees
            var multiplier = 0;
            if (selections.All(x => x == "🎄"))
            {
                multiplier = 30;
            }
            else if (selections.All(x => x == selections[0]))
            {
                multiplier = 10;
            }
            else if (selections.Count(x => x == "💯") > 0)
            {
                multiplier = selections.Count(x => x == "💯");
            }

            var payout = bet * multiplier;

            guser.Points = guser.Points + payout;
            guser.TotalWon = guser.TotalWon + payout;
            guser.TotalBet = guser.TotalBet + bet;

            GameService.SaveGameUser(guser);

            var embed = new EmbedBuilder
            {
                Title = "SLOTS",
                Description = $"➡️ {selections[0]}{selections[1]}{selections[2]} ⬅️\n\n" +
                $"BET: {bet} Points\n" +
                $"PAY: {payout} Points\n" +
                $"BAL: {guser.Points} Points",
                ThumbnailUrl = Context.User.GetAvatarUrl(),
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                Text = $"{Context.User.Username}#{Context.User.Discriminator}"
                }
            };
            await ReplyAsync("", false, embed.Build());
        }
    }
}