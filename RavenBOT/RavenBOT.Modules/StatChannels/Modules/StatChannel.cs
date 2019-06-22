using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common.Attributes;
using RavenBOT.Modules.StatChannels.Methods;

namespace RavenBOT.Modules.StatChannels.Modules
{
    [Group("Tracking")]
    [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
    [RavenRequireBotPermission(Discord.GuildPermission.ManageChannels)]
    [RavenRequireContext(ContextType.Guild)]    
    [Remarks("Requires administrator permissions & bot manage channels permissions")]
    public class StatChannel : InteractiveBase<ShardedCommandContext>
    {
        public StatChannel(StatChannelService service)
        {
            Service = service;
        }

        public StatChannelService Service { get; }

        [Command("ToggleMemberCount")]
        public async Task ToggleMemberCountAsync()
        {
            var config = Service.GetOrCreateConfig(Context.Guild.Id);
            if (config.UserCountChannelId == 0 || Context.Guild.GetVoiceChannel(config.UserCountChannelId) == null)
            {
                var newChannel = await Context.Guild.CreateVoiceChannelAsync($"👥 Members: {Context.Guild.MemberCount}", x => x.Position = (Context.Channel as SocketGuildChannel).Position);

                await newChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new Discord.OverwritePermissions(connect: Discord.PermValue.Deny));
                await ReplyAsync($"I have created a new channel to track the member count in. {newChannel.Name}");
                config.UserCountChannelId = newChannel.Id;
                Service.SaveConfig(config);
            }
            else
            {
                config.UserCountChannelId = 0;
                var channel = Context.Guild.GetVoiceChannel(config.UserCountChannelId);
                await channel.DeleteAsync();
                await ReplyAsync("I have deleted the channel.");
                Service.SaveConfig(config);
            }

        }
    }
}