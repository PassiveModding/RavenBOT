using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;

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

            var response = $"{player.DisplayName} Stats\n" +
                            $"Points: {player.Points}\n"+
                            $"Registered At: {player.RegistrationDate.ToShortDateString()} {player.RegistrationDate.ToShortTimeString()}\n"+
                            $"{player.AdditionalProperties.Select(x => $"{x.Key}: {x.Value}")}\n";

            await ReplyAsync("", false, response.QuickEmbed());
        }
    }
}