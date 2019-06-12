using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.ELO.Models;

namespace RavenBOT.Modules.ELO.Methods
{
    public partial class ELOService
    {
        public async Task UpdateUserAsync(CompetitionConfig comp, Player player, SocketGuildUser user)
        {
            if (user.Guild.CurrentUser.GuildPermissions.Administrator)
            {
                var rankMatches = comp.Ranks.Where(x => x.Points <= player.Points);
                if (rankMatches.Any())
                {
                    var maxRank = rankMatches.Max(x => x.Points);
                    var match = rankMatches.First(x => x.Points == maxRank);

                    if (!user.Roles.Any(x => x.Id == match.RoleId))
                    {
                        var role = user.Guild.GetRole(match.RoleId);
                        if (role.Position < user.Guild.CurrentUser.Hierarchy)
                        {
                            if (role != null)
                            {
                                await user.AddRoleAsync(role);
                            }
                            else
                            {
                                comp.Ranks.Remove(match);
                                Database.Store(comp, CompetitionConfig.DocumentName(comp.GuildId));
                            }  
                        }                    
                    }              
                }
                
                if (comp.RegisteredRankId != 0)
                {
                    if (!user.Roles.Any(x => x.Id == comp.RegisteredRankId))
                    {
                        var role = user.Guild.GetRole(comp.RegisteredRankId);
                        if (role != null)
                        {
                            if (role.Position < user.Guild.CurrentUser.Hierarchy)
                            {
                                await user.AddRoleAsync(role);
                            }
                        }                    
                    }
                }
            }

            var newName = DoReplacements(comp, player);
            if (!user.Nickname.Equals(newName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (user.Guild.CurrentUser.GuildPermissions.ManageNicknames)
                {
                    await user.ModifyAsync(x => x.Nickname = newName);                    
                }
            }
        }

        public string DoReplacements(CompetitionConfig comp, Player player)
        {
            return comp.NameFormat.Replace("{score}", player.Points.ToString(), StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{name}", player.DisplayName, StringComparison.InvariantCultureIgnoreCase).FixLength(32);
        }
    }
}