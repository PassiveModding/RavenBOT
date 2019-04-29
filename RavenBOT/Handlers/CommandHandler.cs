using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Models;
using RavenBOT.Services.SerializableCommandFramwrork;

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
                /*
                var gMatch = CmdService.Trees.FirstOrDefault(x => x.GuildId == context.Guild.Id);
                var ignoreUnknownCommand = false;
                if (gMatch != null)
                {
                    //TODO: Serialization and deserialization
                    var executionResult = (await gMatch.Execute(context, argPos));
                    if (executionResult.OverrideCommand)
                    {
                        return;
                    }

                    ignoreUnknownCommand = executionResult.Success;
                }
                */
                
                
                var result = await CommandService.ExecuteAsync(context, argPos, Provider);

                if (!result.IsSuccess)
                {
                    /*
                    if (result.Error == CommandError.UnknownCommand)
                    {
                        if (ignoreUnknownCommand)
                        {
                            return;
                        }
                    }
                    */
                    Logger.Log(context.Message.Content + "\n" + result.ErrorReason, new LogContext(context), LogSeverity.Error);
                }
                else
                {
                    Logger.Log(context.Message.Content, new LogContext(context));
                }
            }
        }

        
        public JsonCommandService CmdService = new JsonCommandService
        {
            Trees = new List<JsonCommandService.JsonNodeTree>
            {
                new JsonCommandService.JsonNodeTree("testJsonCommandService", new List<JsonCommandService.JsonNodeTree.IJsonNode>
                {
                    new JsonCommandService.JsonNodeTree.BooleanNode("1", "success", "fail", new ConditionExecution.Comparator(), $"{{{new GuildMessageReplacementTypes.CurrentChannelId()}}}", "440036982014345216"),
                    new JsonCommandService.JsonNodeTree.MessageSendActionNode("success", null, "Success message"),
                    new JsonCommandService.JsonNodeTree.MessageSendActionNode("fail", "EndNode", "Fail message"),
                    new JsonCommandService.JsonNodeTree.EndNode("EndNode")
                }, "1", 431613488985538560)
            }
        };
    }
}
