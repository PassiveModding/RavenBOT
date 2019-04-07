using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Models;

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

            int argPos = 0;
            var context = new ShardedCommandContext(Client, message);
            if (message.HasStringPrefix(PrefixService.GetPrefix(context.Guild?.Id ?? 0), ref argPos) || message.HasMentionPrefix(context.Client.CurrentUser, ref argPos))
            {
                
                var result = await CommandService.ExecuteAsync(context, argPos, Provider);

                if (!result.IsSuccess)
                {
                    Logger.Log(context.Message.Content + "\n" + result.ErrorReason, new LogContext(context), LogSeverity.Error);
                }
                else
                {
                    Logger.Log(context.Message.Content, new LogContext(context), LogSeverity.Info);
                }
            }
        }
    }
}
