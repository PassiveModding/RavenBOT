using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.Levels.Methods;
using RavenBOT.Modules.Levels.Models;

namespace RavenBOT.Modules.Levels.Modules
{
    [Group("Level")]
    [RequireContext(ContextType.Guild)]
    public class Level : InteractiveBase<ShardedCommandContext>
    {
        public Level(LevelService levelService)
        {
            LevelService = levelService;
        }

        public LevelService LevelService { get; }

        [Command("Toggle")]
        [Summary("Toggles leveling in the server")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ToggleLevelingAsync()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            config.Enabled = !config.Enabled;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Leveling Enabled: {config.Enabled}");
        }

        [Command("ShowSettings")]
        [Summary("Displays all available level settings")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ShowSettings()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("Leveling has not been enabled in this server.");
                return;
            }

            var responses = config.RewardRoles.OrderByDescending(x => x.LevelRequirement).Select(x =>
            {
                var role = Context.Guild.GetRole(x.RoleId);
                if (role == null)
                {
                    return null;
                }

                return $"{x.LevelRequirement} | {role.Mention}";
            }).Where(x => x != null).ToList();

            var message = $"Enabled: {config.Enabled}\n" +
                        $"Multiple Role rewards: {config.MultiRole}\n" +
                        $"Reply with level ups: {config.ReplyLevelUps}\n" +
                        $"Restriction Mode: {config.RestrictionMode}\n" +
                        $"Restricted Channels:" + string.Join("\n", config.RestrictedChannels.Select(x => Context.Guild.GetTextChannel(x)?.Name ?? $"DELETED: [{x}]")) + 
                        "\nNOTE: If there are deleted channels you can remove them by running the clearchannels command\n" +
                        $"Role Rewards: \n{(responses.Any() ? string.Join("\n", responses) : "N/A" )}\n";

            await ReplyAsync("", false, message.QuickEmbed());
        }

        [Command("ToggleWhitelist")]
        [Summary("Toggles the whitelisting of specific channels for leveling")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ToggleWhitelist()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            config.RestrictionMode = LevelConfig.LevelRestrictionType.Whitelist;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Leveling will now be restricted to channels added with the `addchannel` command");
        }

        [Command("ToggleBlacklist")]
        [Summary("Toggles the Blacklisting of specific channels for leveling")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ToggleBlacklist()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            config.RestrictionMode = LevelConfig.LevelRestrictionType.Blacklist;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Leveling will now be ignored in channels added with the `addchannel` command");
        }
        
        [Command("AddChannel")]
        [Summary("Adds a blacklist or whitelist specific channel")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddChannel()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            if (config.RestrictedChannels.Contains(Context.Channel.Id))
            {
                await ReplyAsync("This channel is already restricted.");
                return;
            }
            if (config.RestrictionMode == LevelConfig.LevelRestrictionType.None)
            {
                await ReplyAsync("You must enable either the blacklist or whitelist to use this command.");
                return;
            }

            config.RestrictedChannels.Add(Context.Channel.Id);
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"This channel is now added to the level {config.RestrictionMode}.");
        }

        [Command("ClearChannels")]
        [Summary("Clears all blacklist or whitelist specific channels")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveChannels()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            if (config.RestrictionMode == LevelConfig.LevelRestrictionType.None)
            {
                await ReplyAsync("You must enable either the blacklist or whitelist to use this command.");
                return;
            }

            config.RestrictedChannels = new List<ulong>();
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Restricted channels have been cleared.");
        }

        [Command("ShowChannnels")]
        [Summary("Shows all blacklist or whitelist specific channels")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ShowChannnels()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            if (config.RestrictionMode == LevelConfig.LevelRestrictionType.None)
            {
                await ReplyAsync("You must enable either the blacklist or whitelist to use this command.");
                return;
            }

            if (!config.RestrictedChannels.Any())
            {
                await ReplyAsync("There are no restricted channels.");
                return;
            }

            var message = string.Join("\n", config.RestrictedChannels.Select(x => Context.Guild.GetTextChannel(x)?.Name ?? $"DELETED: [{x}]")) + 
                        "\nNOTE: If there are deleted channels you can remove them by running the clearchannels command";

            await ReplyAsync("", false, message.QuickEmbed());
        }

        [Command("RemoveChannel")]
        [Summary("Removes a blacklist or whitelist specific channel")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveChannel()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);
            if (config.RestrictionMode == LevelConfig.LevelRestrictionType.None)
            {
                await ReplyAsync("You must enable either the blacklist or whitelist to use this command.");
                return;
            }

            config.RestrictedChannels.Remove(Context.Channel.Id);
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"This channel is now removed from the level {config.RestrictionMode}.");
        }

        [Command("SetLogChannel")]
        [Summary("Sets the channel for level logging")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task Enable()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            config.LogChannelId = Context.Channel.Id;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Level up events will be logged to: {Context.Channel.Name}");
        }

        [Command("DisableLogChannel")]
        [Summary("Disables the level log channel")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task DisableLogChannel()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            config.LogChannelId = 0;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Log channel disabled.");
        }

        [Command("ToggleMessages")]
        [Summary("Toggles the use of level up messages")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ToggleLevelUpNotifications()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            config.ReplyLevelUps = !config.ReplyLevelUps;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Reply Level Ups: {config.ReplyLevelUps}");
        }

        [Command("ToggleMultiRole")]
        [Summary("Toggles whether users keep all level roles or just the highest")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ToggleMultiRole()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            config.MultiRole = !config.MultiRole;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Multiple role rewards: {config.MultiRole}");
        }

        [Command("AddRoleReward")]
        [Summary("Adds a role for a user to earn")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddRole(IRole role, int level)
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            //Remove the role if it already exists in the rewards
            config.RewardRoles = config.RewardRoles.Where(x => x.RoleId != role.Id).ToList();

            if (config.RewardRoles.Any(x => x.LevelRequirement == level))
            {
                await ReplyAsync("There can only be one role per level.");
                return;
            }

            config.RewardRoles.Add(new LevelConfig.LevelReward
            {
                RoleId = role.Id,
                    LevelRequirement = level
            });
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Role Added");
        }

        [Command("AddRoleReward")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddRole(int level, IRole role)
        {
            await AddRole(role, level);
        }

        [Command("RemoveRoleReward")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveRole(IRole role)
        {
            await RemoveRole(role.Id);
        }

        [Command("RemoveRoleReward")]
        [Summary("Removes a role from the role rewards")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveRole(ulong roleId)
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("You must enable leveling before editing it's settings.");
                return;
            }

            //Remove the role if it already exists in the rewards
            config.RewardRoles = config.RewardRoles.Where(x => x.RoleId != roleId).ToList();
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Role Removed\nNOTE: Users who have this role will still keep it.");
        }

        [Command("Rank")]
        [Alias("Level")]
        [Summary("Displays the current (or specified) user's level")]
        public async Task Rank(SocketGuildUser user = null)
        {
            if (user == null)
            {
                user = Context.User as SocketGuildUser;
            }
            var config = LevelService.GetLevelUser(Context.Guild.Id, user.Id);

            if (config == null || config.Item2 == null || !config.Item2.Enabled)
            {
                await ReplyAsync("Leveling is not enabled in this server.");
                return;
            }

            var users = LevelService.Database.Query<LevelUser>().Where(x => x.GuildId == Context.Guild.Id).OrderByDescending(x => x.UserXP).ToList();
            int rank = users.IndexOf(users.Where(x => x.UserId == user.Id).FirstOrDefault()) + 1;

            var embed = new EmbedBuilder()
            {
                Title = "Level Info",
                Author = new EmbedAuthorBuilder()
                {
                IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
                Name = user.ToString()
                },
                Description = $"**Current Level:** {config.Item1.UserLevel}\n" +
                $"**Current XP:** {config.Item1.UserXP}\n" +
                $"**XP To Next Level:** {LevelService.RequiredExp(config.Item1.UserLevel + 1) - config.Item1.UserXP}\n" +
                $"**Rank:** {rank}/{users.Count}"
            };

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Rewards")]
        [Summary("Shows role rewards for reaching a specific level.")]
        public async Task Rewards()
        {
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null || !config.Enabled)
            {
                await ReplyAsync("Leveling is not enabled in this server.");
                return;
            }

            if (!config.RewardRoles.Any())
            {
                await ReplyAsync("There are no reward roles configured for this server.");
                return;
            }

            var responses = config.RewardRoles.OrderByDescending(x => x.LevelRequirement).Select(x =>
            {
                var role = Context.Guild.GetRole(x.RoleId);
                if (role == null)
                {
                    return null;
                }

                return $"{x.LevelRequirement} | {role.Mention}";
            }).Where(x => x != null).ToList();

            if (!responses.Any())
            {
                await ReplyAsync("There are no reward roles configured for this server.");
                return;
            }

            await ReplyAsync($"", false, new EmbedBuilder()
            {
                Title = "Role Rewards",
                    Description = string.Join("\n", responses).FixLength(2047),
                    Color = Color.Blue
            }.Build());
        }

        [Command("Leaderboard")]
        [Summary("Displays users ranked from highest xp to lowest")]
        public async Task ShowLeaderboard()
        {
            var users = LevelService.Database.Query<LevelUser>().Where(x => x.GuildId == Context.Guild.Id).OrderByDescending(x => x.UserXP).ToList();
            if (!users.Any())
            {
                await ReplyAsync("There are no users with levelling in this server.");
                return;
            }

            await Context.Guild.DownloadUsersAsync();

            var pages = new List<PaginatedMessage.Page>();
            int position = 0;
            foreach (var userGroup in users.SplitList(15))
            {
                var page = new PaginatedMessage.Page();
                var userText = userGroup.Select(x =>
                {
                    var user = Context.Guild.GetUser(x.UserId);
                    if (user == null)
                    {
                        return null;
                    }
                    position++;
                    return $"#{position} | {user.Mention} XP:{x.UserXP} LV:{x.UserLevel}";
                }).Where(x => x != null);
                page.Description = string.Join("\n", userText);
                pages.Add(page);
            }

            var pager = new PaginatedMessage
            {
                Title = "Leaderboard",
                Pages = pages,
                Color = Color.Blue
            };

            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                    Backward = true,
                    Jump = true,
                    First = true,
                    Last = true
            });
        }

        [Command("ResetUser")]
        [Summary("Resets the specified user's level stats")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetLeaderboard(SocketGuildUser user)
        {
            if (user.Hierarchy >= (Context.User as SocketGuildUser).Hierarchy)
            {
                if (user.Id != Context.User.Id)
                {
                    await ReplyAsync("You cannot reset the level of a user with higher permissions than you.");
                    return;
                }
            }

            var levelUser = LevelService.GetLevelUser(Context.Guild.Id, user.Id);
            if (levelUser.Item1 != null)
            {
                var profile = levelUser.Item1;
                profile.UserLevel = 0;
                profile.UserXP = 0;
                LevelService.Database.Store(profile, LevelUser.DocumentName(profile.UserId, profile.GuildId));
                await ReplyAsync("User has been reset.");
            }
            else
            {
                await ReplyAsync("User does not exist in the level database.");
            }
        }

        [Command("ResetLeaderboard")]
        [Summary("Resets all level stats for users in the server")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetLeaderboard(string confirm = null)
        {
            if (confirm == null)
            {
                await ReplyAsync("Run this command again with the confirmation code `cop432ih`\n" +
                    "This will remove all earned xp from users and **CANNOT** be undone\n" +
                    "NOTE: It will not remove earned roles from users.");
                return;
            }
            else if (!confirm.Equals("cop432ih"))
            {
                await ReplyAsync("Invalid confirmation code.");
                return;
            }
            var users = LevelService.Database.Query<LevelUser>().Where(x => x.GuildId == Context.Guild.Id).ToList();
            LevelService.Database.RemoveMany<LevelUser>(users.Select(x => LevelUser.DocumentName(x.UserId, x.GuildId)).ToList());
            await ReplyAsync("Leaderboard has been reset.");
        }

        [Command("RebaseXPViaRewards")]
        [Summary("Sets user xp based on the current level role they have.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RebaseXp()
        {
            var users = LevelService.Database.Query<LevelUser>().Where(x => x.GuildId == Context.Guild.Id).ToList();
            var config = LevelService.TryGetLevelConfig(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("There are no configured roles.");
                return;
            }

            await Context.Guild.DownloadUsersAsync();

            var updatedUsers = new List<LevelUser>();

            foreach (var role in config.RewardRoles.OrderByDescending(x => x.LevelRequirement))
            {
                var serverRole = Context.Guild.GetRole(role.RoleId);
                if (serverRole == null)
                {
                    continue;
                }

                foreach (var user in serverRole.Members)
                {
                    if (updatedUsers.Any(x => x.UserId == user.Id))
                    {
                        //Skip users who were already modified because we are working from highest to lowest.
                        continue;
                    }

                    var leveluser = users.FirstOrDefault(x => x.UserId == user.Id);
                    if (leveluser == null)
                    {
                        leveluser = new LevelUser(user.Id, Context.Guild.Id);
                    }

                    leveluser.UserLevel = role.LevelRequirement;
                    leveluser.UserXP = LevelService.RequiredExp(role.LevelRequirement);

                    updatedUsers.Add(leveluser);
                }
            }
            LevelService.Database.StoreMany(updatedUsers, x => LevelUser.DocumentName(x.UserId, x.GuildId));

            await ReplyAsync($"{updatedUsers.Count} users have been updated");
        }
    }
}