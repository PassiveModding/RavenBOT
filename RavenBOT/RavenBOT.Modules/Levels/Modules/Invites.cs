using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Modules.Levels.Methods;
using RavenBOT.Modules.Levels.Models;

namespace RavenBOT.Modules.Levels.Modules
{
    public partial class Level
    {
        public async Task UserJoinedAsync(SocketGuildUser user)
        {
            var _ = Task.Run(async() =>
            {
                if (!user.Guild.CurrentUser.GuildPermissions.ManageGuild)
                {
                    return;
                }

                var levelConfig = LevelService.TryGetLevelConfig(user.Guild.Id);
                if (levelConfig == null || !levelConfig.Enabled)
                {
                    return;
                }

                var guildObj = LevelService.Database.Load<LevelInviteTracker>(LevelInviteTracker.DocumentName(user.Guild.Id));
                if (guildObj == null || !guildObj.Enabled)
                {
                    return;
                }

                var trackedMatches = new List<TrackedInvite>();

                foreach (var invite in await Context.Guild.GetInvitesAsync())
                {
                    //Ensure the invite is being tracked
                    if (!guildObj.TrackedInvites.TryGetValue(invite.Code, out var trackedInvite))
                    {
                        trackedInvite = new TrackedInvite
                        {
                            InviteCode = invite.Code,
                            JoinCount = invite.Uses ?? 0,
                            CreatorId = invite.Inviter.Id
                        };
                        guildObj.TrackedInvites.Add(invite.Code, trackedInvite);
                    }

                    //Ensure invite uses is specified
                    //Ensure user has not joined the server under any other tracked invite.
                    if (invite.Uses.HasValue && trackedInvite.JoinCount < invite.Uses.Value && !guildObj.TrackedInvites.Any(x => x.Value.TrackedUsers.Contains(user.Id)))
                    {
                        trackedInvite.JoinCount = invite.Uses.Value;
                        trackedInvite.TrackedUsers.Add(user.Id);
                        trackedMatches.Add(trackedInvite);
                        guildObj.TrackedInvites[trackedInvite.InviteCode] = trackedInvite;
                    }
                }

                if (trackedMatches.Count > 1 || trackedMatches.Count == 0)
                {
                    return;
                }

                var match = trackedMatches.First();
                var inviter = user.Guild.GetUser(match.CreatorId);
                if (inviter == null)
                {
                    return;
                }

                var levelUser = LevelService.GetLevelUser(user.Guild.Id, match.CreatorId);
                levelUser.Item1.UserXP += 100;
                LevelService.Database.Store(levelUser.Item1, LevelUser.DocumentName(levelUser.Item1.UserId, levelUser.Item1.GuildId));

                LevelService.Database.Store(guildObj, LevelInviteTracker.DocumentName(user.Guild.Id));
            });

        }

        public class TrackedInvite
        {
            public string InviteCode { get; set; }
            public ulong CreatorId { get; set; }
            public int JoinCount { get; set; }

            public List<ulong> TrackedUsers { get; set; } = new List<ulong>();
        }

        [Command("ToggleInviteXP")]
        [Summary("Allows users to earn 100xp for every user that is invited through one of their invites.")]
        [RavenRequireContext(ContextType.Guild)]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        [RavenRequireBotPermission(Discord.GuildPermission.ManageGuild)]
        public async Task AllowInviteExp()
        {
            var guildConfig = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (guildConfig == null || !guildConfig.Enabled)
            {
                await ReplyAsync("Leveling must be enabled before enabling invite levels.");
                return;
            }

            var guildObj = LevelService.Database.Load<LevelInviteTracker>(LevelInviteTracker.DocumentName(Context.Guild.Id));
            if (guildObj == null)
            {
                guildObj = new LevelInviteTracker();
                guildObj.GuildId = Context.Guild.Id;
                guildObj.Enabled = true;
            }
            else
            {
                guildObj.Enabled = !guildObj.Enabled;
            }

            LevelService.Database.Store(guildObj, LevelInviteTracker.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Level Invites: {guildObj.Enabled}");
        }
    }
}