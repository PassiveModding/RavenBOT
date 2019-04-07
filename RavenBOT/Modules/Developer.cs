using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Handlers;

namespace RavenBOT.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Developer : ModuleBase<SocketCommandContext>
    {
        public LogHandler Logger { get; }

        public Developer(LogHandler logger)
        {
            Logger = logger;
        }

        [Command("SetLoggerChannel")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task SetLoggerChannelAsync(SocketTextChannel channel = null)
        {
            var originalConfig = Logger.GetLoggerConfig();
            if (channel == null)
            {
                await ReplyAsync("Removed logger channel");
                originalConfig.ChannelId = 0;
                originalConfig.GuildId = 0;
                originalConfig.LogToChannel = false;
                Logger.SetLoggerConfig(originalConfig);
                return;
            }

            originalConfig.GuildId = channel.Guild.Id;
            originalConfig.ChannelId = channel.Id;
            originalConfig.LogToChannel = true;
            Logger.SetLoggerConfig(originalConfig);
            await ReplyAsync($"Set Logger channel to {channel.Mention}");
        }
    }
}
