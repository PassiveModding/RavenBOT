using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        public ELOService Service { get; }

        public Info(ELOService service)
        {
            Service = service;
        }

        [Command("Register")]
        public async Task RegisterAsync([Remainder]string name = null)
        {
            if (name == null)
            {
                name = (Context.User as SocketGuildUser)?.Nickname ?? Context.User.Username;
            }

            //TODO: Add option to prevent re-registering
            //TODO: Add precondition for premium  
            //TODO: Fix name not being set when re-registering

            var player = Service.GetPlayer(Context.Guild.Id, Context.User.Id) ?? Service.CreatePlayer(Context.Guild.Id, Context.User.Id, name);
            var competition = Service.GetCompetition(Context.Guild.Id);
            var responses = await Service.UpdateUserAsync(competition, player, Context.User as SocketGuildUser);
            await ReplyAsync($"You have registered as `{name}`, all roles/name updates have been applied if applicable.");
            if (responses.Count > 0)
            {
                await ReplyAndDeleteAsync("", false, String.Join("\n", responses).QuickEmbed(), TimeSpan.FromSeconds(30));
            }
        }

        [Command("Ranks")]
        public async Task ShowRanksAsync()
        {
            var comp = Service.GetCompetition(Context.Guild.Id);
            if (!comp.Ranks.Any())
            {
                await ReplyAsync("There are currently no ranks set up.");
                return;
            }

            var msg = comp.Ranks.OrderByDescending(x => x.Points).Select(x => $"{Context.Guild.GetRole(x.RoleId)?.Mention ?? $"[{x.RoleId}]"} - {x.Points}").ToArray();
            await ReplyAsync("", false, string.Join("\n", msg).QuickEmbed());
        }

        [Command("Info")]
        public async Task InfoAsync(SocketGuildUser user = null)
        {
            if (user == null)
            {
                user = Context.User as SocketGuildUser;
            }

            var player = Service.GetPlayer(Context.Guild.Id, user.Id);
            if (player == null)
            {
                await ReplyAsync("You are not registered.");
                return;
            }
            
            var competition = Service.GetCompetition(Context.Guild.Id);
            var rank = competition.MaxRank(player.Points);
            string rankStr = null;
            if (rank != null)
            {
                var guildRole = Context.Guild.GetRole(rank.RoleId);
                if (guildRole != null)
                {
                    rankStr = $"Rank: {guildRole.Mention} ({rank.Points})\n";
                }
            }

            var response = $"{player.DisplayName} Stats\n" +
                            $"Points: {player.Points}\n"+
                            rankStr +
                            $"Wins: {player.Wins}\n"+
                            $"Losses: {player.Losses}\n"+
                            $"Draws: {player.Draws}\n"+
                            $"Games: {player.Games}\n"+
                            $"Registered At: {player.RegistrationDate.ToShortDateString()} {player.RegistrationDate.ToShortTimeString()}\n"+
                            $"{player.AdditionalProperties.Select(x => $"{x.Key}: {x.Value}")}\n";

            await ReplyAsync("", false, response.QuickEmbed());
        }

        [Command("Leaderboard")]
        public async Task LeaderboardAsync()
        {
            //TODO: Implement sort modes

            //Retrieve players in the current guild from database
            var users = Service.Database.Query<Player>(x => x.GuildId == Context.Guild.Id);

            //Order players by score and then split them into groups of 20 for pagination
            var userGroups = users.OrderByDescending(x => x.Points).SplitList(20).ToArray();

            //Convert the groups into formatted pages for the response message
            var pages = GetPages(userGroups);

            //Construct a paginated message with each of the leaderboard pages
            var pager = new PaginatedMessage();
            pager.Pages = pages;
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                First = true,
                Last = true
            });
        }

        public List<PaginatedMessage.Page> GetPages(IEnumerable<Player>[] groups)
        {
            //Start the index at 1 because we are ranking players here ie. first place.
            int index = 1;
            var pages = new List<PaginatedMessage.Page>(groups.Length);
            foreach (var group in groups)
            {
                var playerGroup = group.ToArray();
                var lines = GetPlayerLines(playerGroup, index);
                index = lines.Item1;
                var page = new PaginatedMessage.Page();
                page.Description = lines.Item2;
                pages.Add(page);
            }

            return pages;
        }

        //Returns the updated index and the formatted player lines
        public (int, string) GetPlayerLines(Player[] players, int startValue)
        {
            var sb = new StringBuilder();
            
            //Iterate through the players and add their summary line to the list.
            foreach (var player in players)
            {
                sb.AppendLine($"{startValue}: {player.DisplayName} - {player.Points}");
                startValue++;
            }

            //Return the updated start value and the list of player lines.
            return (startValue, sb.ToString());
        }
    }
}