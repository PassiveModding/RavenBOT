using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Levels.Methods;
using RavenBOT.Modules.Levels.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Levels.Modules
{
    [Group("Level.")]
    [RequireContext(ContextType.Guild)]
    public class Level : InteractiveBase<ShardedCommandContext>
    {
        public Level(IDatabase database, DiscordShardedClient client)
        {
            LevelService = new LevelService(database, client);
        }

        public LevelService LevelService { get; }

        [Command("Toggle")]
        public async Task ToggleLevelingAsync()
        {
            var config = LevelService.GetOrCreateLevelConfig(Context.Guild.Id);   
            config.Enabled = !config.Enabled;
            LevelService.Database.Store(config, LevelConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Leveling Enabled: {config.Enabled}");
        }

        [Command("SetLogChannel")]
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
        public async Task AddRole(int level, IRole role)
        {
            await AddRole(role, level);
        }

        [Command("RemoveRoleReward")]
        public async Task RemoveRole(IRole role)
        {
            await RemoveRole(role.Id);
        }  

        [Command("RemoveRoleReward")]
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
    }
}