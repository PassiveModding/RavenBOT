using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;

namespace RavenBOT.Core.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [RavenRequireUserPermission(GuildPermission.Administrator)]
    [Group("config")]
    public class GuildConfiguration : ReactiveBase
    {
        protected override void BeforeExecute(CommandInfo info)
        {
            Command = info;
        }

        private CommandInfo Command;
        public GuildService GuildService { get; }

        public GuildConfiguration(GuildService guildService)
        {
            GuildService = guildService;
        }

        [Command("UnknownCommandResponse")]
        [Summary("Toggle the unknown command response message")]
        public async Task ToggleUnknownCommandAsync()
        {
            var currentConfig = GuildService.GetOrCreateConfig(Context.Guild.Id);
            currentConfig.DisplayUnknownCommandResponse = !currentConfig.DisplayUnknownCommandResponse;
            await ReplyAsync($"Ignore Unknown Command Responses: {currentConfig.DisplayUnknownCommandResponse}");
            GuildService.SaveConfig(currentConfig);
        }

        [Command("SetGuildPrefix")]
        public async Task SetGuildPrefixAsync([Remainder] string prefix)
        {
            await ReplyAsync($"Guild Prefix set to: {prefix}");
            var config = GuildService.GetOrCreateConfig(Context.Guild.Id);
            config.PrefixOverride = prefix;
            GuildService.SaveConfig(config);
        }

        [Command("RemoveGuildPrefix")]
        public async Task ResetGuildPrefixAsync()
        {
            await ReplyAsync($"Guild Prefix set to: {GuildService.DefaultPrefix}");
            var config = GuildService.GetConfig(Context.Guild.Id);
            if (config == null) return;
            config.PrefixOverride = null;
            GuildService.SaveConfig(config);
        }

        [Command("AddToBlacklist")]
        public async Task AddToBlacklist(string name)
        {
            if (name.StartsWith(Command.Module.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync("It would be a bad idea to blacklist this module don't you think.");
                return;
            }

            var config = GuildService.GetOrCreateConfig(Context.Guild.Id);
            config.ModuleBlacklist.Add(name);
            GuildService.SaveConfig(config);
            await ReplyAsync($"Added {name} to the blacklisted command/module list");
        }

        [Command("ClearBlacklist")]
        public async Task ClearBlacklist()
        {
            await ReplyAsync($"Cleared the module/command blacklist");

            var config = GuildService.GetConfig(Context.Guild.Id);
            if (config == null) return;
            config.ModuleBlacklist.Clear();
            GuildService.SaveConfig(config);
        }

        [Command("ShowBlacklist")]
        public async Task ShowBlacklist()
        {
            var config = GuildService.GetConfig(Context.Guild.Id);
            if (config == null || config.ModuleBlacklist.Count == 0) 
            {
                await ReplyAsync("Blacklist is empty.");
                return;
            }

            await ReplyAsync(string.Join("\n", config.ModuleBlacklist));
        }

        [Command("RemoveFromBlacklist")]
        public async Task RemoveFromBlacklist(string name)
        {
            var config = GuildService.GetConfig(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("No Blacklisted items to remove.");
                return;
            }

            if (config.ModuleBlacklist.Remove(name))
            {
                await ReplyAsync($"Removed {name} from the blacklisted command/module list");
                GuildService.SaveConfig(config);
            }
            else
            {
                await ReplyAsync("The blacklist does not contain anything with this name. Note that is is case sensitive.\nYou can the list of blacklisted items by using the `ShowBlacklist` command");
            }
        }
    }
}