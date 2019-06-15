using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Extensions;
using RavenBOT.Modules.Media.Methods;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("Media Channel")]
    [RequireContext(ContextType.Guild)]
    public class MediaRestrictor : InteractiveBase<ShardedCommandContext>
    {
        public MediaRestrictor(MediaRestrictorHelper helper)
        {
            Helper = helper;
        }

        public MediaRestrictorHelper Helper { get; }

        [Command("Make Channel")]
        public async Task MakeMediaRestrictedChannel(params IRole[] roles)
        {
            var channelConfig = Helper.GetOrCreateConfig(Context.Channel.Id);
            channelConfig.WhitelistedRoleIds = roles.Select(x => x.Id).ToList();
            Helper.SaveConfig(channelConfig);
            await ReplyAsync("Media channel created.");
        }

        [Command("Remove Channel")]
        public async Task RemoveMediaRestrictedChannel()
        {
            var channelConfig = Helper.GetConfig(Context.Channel.Id);
            if (channelConfig != null)
            {
                Helper.Database.Remove<MediaRestrictorHelper.MediaRestrictedChannel>(MediaRestrictorHelper.MediaRestrictedChannel.DocumentName(Context.Channel.Id));
                await ReplyAsync("Channel Removed.");
            }
        }

        [Command("Whitelist Roles")]
        public async Task WhiltelistRoles(params IRole[] roles)
        {
            var channelConfig = Helper.GetConfig(Context.Channel.Id);
            if (channelConfig == null)
            {
                await ReplyAsync("Channel is not a media channel.");
                return;
            }
            channelConfig.WhitelistedRoleIds = roles.Select(x => x.Id).ToList();
            Helper.SaveConfig(channelConfig);
            await ReplyAsync("Roles set.");
        }

        [Command("Show Roles")]
        public async Task ShowRoles()
        {
            var channelConfig = Helper.GetConfig(Context.Channel.Id);
            if (channelConfig == null)
            {
                await ReplyAsync("Channel is not a media channel.");
                return;
            }

            if (!channelConfig.WhitelistedRoleIds.Any())
            {
                await ReplyAsync("There are no whitelisted roles.");
                return;
            }

            var roles = Context.Guild.Roles.Where(x => channelConfig.WhitelistedRoleIds.Contains(x.Id));

            await ReplyAsync("", false, string.Join("\n", roles.Select(x => x.Mention)).QuickEmbed());
        }
    }
}