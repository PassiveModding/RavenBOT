using System;
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
    [Preconditions.RequireAdmin]
    public class UserManagement : ReactiveBase
    {
        public UserManagement(ELOService service)
        {
            Service = service;
        }

        public ELOService Service { get; }

        [Command("BanUser", RunMode = RunMode.Sync)]
        public async Task BanUserAsync(TimeSpan time, SocketGuildUser user)
        {
            var player = Service.GetPlayer(Context.Guild.Id, user.Id);
            if (player == null)
            {
                await ReplyAsync("User is not registered.");
                return;
            }

            player.BanExpiry = DateTime.UtcNow + time;
            Service.SavePlayer(player);
            await ReplyAsync($"Player banned from joining games until: {player.BanExpiry.ToShortDateString()} {player.BanExpiry.ToShortTimeString()}");
        }

        [Command("DeleteUser", RunMode = RunMode.Sync)]
        [Alias("DelUser")]
        public async Task DeleteUserAsync(SocketGuildUser user)
        {
            var player = Service.GetPlayer(Context.Guild.Id, user.Id);
            if (player == null)
            {
                await ReplyAsync("User isn't registered.");
                return;
            }

            var competition = Service.GetOrCreateCompetition(Context.Guild.Id);

            //Remove user ranks, register role and nickname
            Service.RemovePlayer(player);

            if (user.Hierarchy < Context.Guild.CurrentUser.Hierarchy)
            {
                if (Context.Guild.CurrentUser.GuildPermissions.ManageRoles)
                {
                    var rolesToRemove = user.Roles.Where(x => competition.Ranks.Any(r => r.RoleId == x.Id)).ToList();
                    if (competition.RegisteredRankId != 0)
                    {
                        var registerRole = Context.Guild.GetRole(competition.RegisteredRankId);
                        if (registerRole != null)
                        {
                            rolesToRemove.Add(registerRole);
                        }
                    }
                    if (rolesToRemove.Any())
                    {
                        await user.RemoveRolesAsync(rolesToRemove);
                    }
                }

                
                if (Context.Guild.CurrentUser.GuildPermissions.ManageNicknames)
                {
                    if (user.Nickname != null)
                    {
                        //TODO: Combine role and nick modification to reduce discord requests
                        await user.ModifyAsync(x => x.Nickname = null);
                    }
                }
            }
            else
            {
                await ReplyAsync("The user being deleted has a higher permission level than the bot and cannot have their ranks or nickname modified.");
            }
        }

    }
}