using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private List<string> ModulePrefixes { get; set; }

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

            var messageContent = message.Content;

            if (Local.Developer)
            {
                //Strip away developer prefix
                if (messageContent.StartsWith(Local.DeveloperPrefix, true, CultureInfo.InvariantCulture))
                {
                    messageContent = messageContent.Substring(Local.DeveloperPrefix.Length);
                }
                else
                {
                    return;
                }
            }
            
            if (BotConfig.UsePrefixSystem)
            {
                int argPos = 0;
                var context = new ShardedCommandContext(Client, message);

                var serverPrefix = PrefixService.GetPrefix(context.Guild?.Id ?? 0);

                var isCommand = false;

                if (messageContent.StartsWith(serverPrefix))
                {
                    argPos = serverPrefix.Length;
                    messageContent = messageContent.Substring(argPos);
                    isCommand = true;
                }
                else if (messageContent.StartsWith(context.Client.CurrentUser.Mention))
                {
                    argPos = context.Client.CurrentUser.Mention.Length;
                    messageContent = messageContent.Substring(argPos);
                    isCommand = true;
                }

                if (isCommand)
                {
                    if (!ModuleManager.IsAllowed(context.Guild?.Id ?? 0, messageContent))
                    {
                        return;
                    }

                    var result = await CommandService.ExecuteAsync(context, messageContent, Provider);

                    if (!result.IsSuccess)
                    {
                        Logger.Log(context.Message.Content + "\n" + result.ErrorReason, new LogContext(context), LogSeverity.Error);
                    }
                    else
                    {
                        Logger.Log(context.Message.Content, new LogContext(context));
                    }
                }
            }
            else
            {
                var prefixMatch = ModulePrefixes.OrderByDescending(x => x.Length).FirstOrDefault(x => messageContent.StartsWith(x, true, CultureInfo.InvariantCulture));


                if (prefixMatch != null)
                {
                    var context = new ShardedCommandContext(Client, message);

                    if (!ModuleManager.IsAllowed(context.Guild?.Id ?? 0, messageContent))
                    {
                        return;
                    }

                    IResult result;

                    if (!string.IsNullOrWhiteSpace(prefixMatch))
                    {            
                        result = await CommandService.ExecuteAsync(context, messageContent.Replace(prefixMatch, $"{prefixMatch} ", true, CultureInfo.InvariantCulture), Provider);
                    }
                    else
                    {
                        result = await CommandService.ExecuteAsync(context, 0, Provider);
                    }                       


                    if (!result.IsSuccess)
                    {
                        Logger.Log(context.Message.Content + "\n" + result.ErrorReason, new LogContext(context), LogSeverity.Error);
                    }
                    else
                    {
                        Logger.Log(context.Message.Content, new LogContext(context));
                    }
                }
            }
        }
    }
}
