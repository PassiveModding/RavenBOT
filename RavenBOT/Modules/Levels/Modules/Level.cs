using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Extensions;
using RavenBOT.Modules.Levels.Methods;
using RavenBOT.Modules.Levels.Models;
using RavenBOT.Services.Database;

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
        public async Task Rank(SocketGuildUser user = null)
        {
            var config = LevelService.GetLevelUser(Context.Guild.Id, user?.Id ?? Context.User.Id);   
            if (config == null || config.Item2 == null || !config.Item2.Enabled)
            {
                await ReplyAsync("Leveling is not enabled in this server.");
                return;
            }


            await ReplyAsync($"Current Level: {config.Item1.UserLevel}\n" +
                            $"Current XP: {config.Item1.UserXP}\n" +
                            $"XP To Next Level: {LevelService.RequiredExp(config.Item1.UserLevel + 1) - config.Item1.UserXP}\n");

            //TODO: Rank
        }    

        
        [Command("Rewards")]        
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

            //TODO: Rank
        }

        [Command("Leaderboard")]
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
    }
}