using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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

        [Command("Bans")]
        public async Task Bans()
        {
            var players = Service.GetPlayers(x => x.GuildId == Context.Guild.Id && x.IsBanned);
            if (players.Length == 0)
            {
                await ReplyAsync("There aren't any banned players.");
                return;
            }
            var pages = players.OrderBy(x => x.CurrentBan.RemainingTime).SplitList(20).Select(x =>
            {
                var page = new ReactivePage();
                page.Description = string.Join("\n", x.Select(p => $"{MentionUtils.MentionUser(p.UserId)} - {p.CurrentBan.ExpiryTime.ToShortDateString()} {p.CurrentBan.ExpiryTime.ToShortTimeString()} in {p.CurrentBan.RemainingTime.GetReadableLength()}"));
                return page;
            });
            var pager = new ReactivePager(pages);
            await PagedReplyAsync(pager.ToCallBack().WithDefaultPagerCallbacks());
        }

        [Command("Unban", RunMode = RunMode.Sync)]
        public async Task Unban(SocketGuildUser user)
        {
            if (!user.IsRegistered(Service, out var player))
            {
                await ReplyAsync("Player is not registered.");
                return;
            }

            player.CurrentBan.ManuallyDisabled = true;
            Service.SavePlayer(player);
            await ReplyAsync("Unbanned player.");
        }

        [Command("BanUser", RunMode = RunMode.Sync)]
        public async Task BanUserAsync(TimeSpan time, SocketGuildUser user, string reason = null)
        {
            var player = Service.GetPlayer(Context.Guild.Id, user.Id);
            if (player == null)
            {
                await ReplyAsync("User is not registered.");
                return;
            }

            player.BanHistory.Add(new Player.Ban(time, Context.User.Id, reason));
            Service.SavePlayer(player);
            await ReplyAsync($"Player banned from joining games until: {player.CurrentBan.ExpiryTime.ToShortDateString()} {player.CurrentBan.ExpiryTime.ToShortTimeString()} in {player.CurrentBan.RemainingTime.GetReadableLength()}");
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