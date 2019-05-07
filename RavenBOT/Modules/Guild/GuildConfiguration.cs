using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Services;

namespace RavenBOT.Modules.Guild
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("config.")]
    public class GuildConfiguration : ModuleBase<SocketCommandContext>
    {
        public PrefixService PrefixService { get; }

        public GuildConfiguration(PrefixService prefixService)
        {
            PrefixService = prefixService;
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
    }
}
