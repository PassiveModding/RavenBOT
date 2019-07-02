using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Modules.Media.Methods;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("Media Channel")]
    [RavenRequireContext(ContextType.Guild)]
    [RavenRequireUserPermission(GuildPermission.Administrator)]
    public partial class MediaRestrictor : InteractiveBase<ShardedCommandContext>
    {
        public MediaRestrictor(MediaRestrictorHelper helper)
        {
            Helper = helper;
        }

        public MediaRestrictorHelper Helper { get; }

        [Command("Make")]
        [Summary("Creates a media channel, only allows urls + attachments in chat")]
        public async Task MakeMediaRestrictedChannel(params IRole[] roles)
        {
            var channelConfig = Helper.GetOrCreateConfig(Context.Channel.Id);
            channelConfig.WhitelistedRoleIds = roles.Select(x => x.Id).ToList();
            Helper.SaveConfig(channelConfig);
            await ReplyAsync("Media channel created.");
        }

        [Command("Remove")]
        [Summary("Removes a media channel")]
        public async Task RemoveMediaRestrictedChannel(SocketTextChannel channel = null)
        {
            var channelConfig = Helper.GetConfig(channel?.Id ?? Context.Channel.Id);
            if (channelConfig != null)
            {
                Helper.Database.Remove<MediaRestrictorHelper.MediaRestrictedChannel>(MediaRestrictorHelper.MediaRestrictedChannel.DocumentName(Context.Channel.Id));
                await ReplyAsync("Channel Removed.");
            }
        }

        [Command("Whitelist Roles")]
        [Alias("WhitelistRoles")]
        [Summary("Allows the specified roles to speak normally in a media channel.")]
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
        [Alias("ShowRoles")]
        [Summary("Displays media channel whitelisted roles")]
        public async Task ShowRoles(SocketTextChannel channel = null)
        {
            var channelConfig = Helper.GetConfig(channel?.Id ?? Context.Channel.Id);
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