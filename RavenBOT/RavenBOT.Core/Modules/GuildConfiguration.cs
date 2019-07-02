using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;

namespace RavenBOT.Modules.Guild
{
    [RavenRequireContext(ContextType.Guild)]
    [RavenRequireUserPermission(GuildPermission.Administrator)]
    [Group("config")]
    public class GuildConfiguration : InteractiveBase<SocketCommandContext>
    {
        protected override void BeforeExecute(CommandInfo info)
        {
            Command = info;
        }

        private CommandInfo Command;

        public PrefixService PrefixService { get; }
        public ModuleManagementService ModuleManager { get; }

        public GuildConfiguration(PrefixService prefixService, ModuleManagementService mms)
        {
            PrefixService = prefixService;
            ModuleManager = mms;
        }

        [Command("SetGuildPrefix")]
        public async Task SetGuildPrefixAsync([Remainder] string prefix)
        {
            await ReplyAsync($"Guild Prefix set to: {prefix}");
            PrefixService.SetPrefix(Context.Guild.Id, prefix);
        }

        [Command("RemoveGuildPrefix")]
        public async Task ResetGuildPrefixAsync()
        {
            await ReplyAsync($"Guild Prefix set to: {PrefixService.GetPrefix(0)}");
            PrefixService.SetPrefix(Context.Guild.Id, null);
        }

        [Command("AddToBlacklist")]
        public async Task AddToBlacklist(string name)
        {
            if (name.StartsWith(Command.Module.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync("It would be a bad idea to blacklist this module don't you think.");
                return;
            }

            var config = ModuleManager.GetModuleConfig(Context.Guild.Id);
            config.Blacklist.Add(name);
            ModuleManager.SaveModuleConfig(config);
            await ReplyAsync($"Added {name} to the blacklisted command/module list");
        }

        [Command("ClearBlacklist")]
        public async Task ClearBlacklist()
        {
            var config = ModuleManager.GetModuleConfig(Context.Guild.Id);
            config.Blacklist = new List<string>();
            ModuleManager.SaveModuleConfig(config);
            await ReplyAsync($"Cleared the module/command blacklist");
        }

        [Command("ShowBlacklist")]
        public async Task ShowBlacklist()
        {
            var config = ModuleManager.GetModuleConfig(Context.Guild.Id);
            await ReplyAsync(string.Join("\n", config.Blacklist) ?? "The blacklist is empty");
        }

        [Command("RemoveFromBlacklist")]
        public async Task RemoveFromBlacklist(string name)
        {
            var config = ModuleManager.GetModuleConfig(Context.Guild.Id);

            if (config.Blacklist.Remove(name))
            {
                await ReplyAsync($"Removed {name} from the blacklisted command/module list");
                ModuleManager.SaveModuleConfig(config);
            }
            else
            {
                await ReplyAsync("The blacklist does not contain anything with this name. Note that is is case sensitive.\nYou can the list of blacklisted items by using the `ShowBlacklist` command");
            }
        }
    }
}