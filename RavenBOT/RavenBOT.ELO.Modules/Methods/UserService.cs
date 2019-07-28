using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Methods
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

        public (ulong, ulong) GetCaptains(Lobby lobby, GameResult game, Random rnd)
        {
            ulong cap1 = 0;
            ulong cap2 = 0;
            if (lobby.TeamPickMode == Lobby.PickMode.Captains_RandomHighestRanked)
            {
                //Select randomly from the top 4 ranked players in the queue
                if (game.Queue.Count >= 4)
                {
                    var players = game.Queue.Select(x => GetPlayer(game.GuildId, x)).Where(x => x != null).OrderByDescending(x => x.Points).Take(4).OrderBy(x => rnd.Next()).ToList();
                    cap1 = players[0].UserId;
                    cap2 = players[1].UserId;
                }
                //Select the two players at random.
                else
                {
                    var randomised = game.Queue.OrderBy(x => rnd.Next()).Take(2).ToList();
                    cap1 = randomised[0];
                    cap2 = randomised[1];
                }
            }
            else if (lobby.TeamPickMode == Lobby.PickMode.Captains_Random)
            {
                //Select two players at random.
                var randomised = game.Queue.OrderBy(x => rnd.Next()).Take(2).ToList();
                cap1 = randomised[0];
                cap2 = randomised[1];
            }
            else if (lobby.TeamPickMode == Lobby.PickMode.Captains_HighestRanked)
            {
                //Select top two players
                var players = game.Queue.Select(x => GetPlayer(game.GuildId, x)).Where(x => x != null).OrderByDescending(x => x.Points).Take(2).ToList();
                cap1 = players[0].UserId;
                cap2 = players[1].UserId;
            }
            else
            {
                throw new Exception("Unknown captain pick mode.");
            }

            return (cap1, cap2);
        }
    }
}