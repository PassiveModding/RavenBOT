using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.Handlers
{
    //Command handling section of the event handler
    public partial class EventHandler
    {
        private async Task MessageReceivedAsync(SocketMessage discordMessage)
        {
            if (!(discordMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }

            ulong guildId = 0;
            if (message.Channel is IGuildChannel gChannel)
            {
                guildId = gChannel.GuildId;
            }

            if (!LocalManagementService.LastConfig.IsAcceptable(guildId))
            {
                return;
            }

            var argPos = 0;
            if (!message.HasStringPrefix(LocalManagementService.LastConfig.Developer ? LocalManagementService.LastConfig.DeveloperPrefix : PrefixService.GetPrefix(guildId), ref argPos, System.StringComparison.InvariantCultureIgnoreCase) /*&& !message.HasMentionPrefix(Client.CurrentUser, ref argPos)*/ )
            {
                return;
            }

            var context = GetCommandContext(Client, message);
            if (!ModuleManager.IsAllowed(context.Guild?.Id ?? 0, message.Content))
            {
                return;
            }

            var result = await CommandService.ExecuteAsync(context, argPos, Provider);
        }

        public Func<DiscordShardedClient, SocketUserMessage, ICommandContext> GetCommandContext = (c, m) => new ShardedCommandContext(c, m);        
    }


}