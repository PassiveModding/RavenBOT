using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Models;
using RavenBOT.Services.SerializableCommandFramework;

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

            if (Local.Developer)
            {
                int argPos = 0;
                
                //Prefix service handles developer prefix however we still need to check if the bot is using prefixes or the module groups.
                if (BotConfig.UsePrefixSystem)
                {
                    //The bot can be used regularly if it's just using regular prefixes
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
                            Logger.Log(context.Message.Content, new LogContext(context));
                        }
                    }
                }
                else
                {
                    var discarded = 0;
                    //Check if the command starts with the dev prefix AND the module prefix.
                    if (ModulePrefixes.Any(x => message.HasStringPrefix(Local.DeveloperPrefix + x, ref discarded)))
                    {
                        var context = new ShardedCommandContext(Client, message);

                        //Find the first match of the module prefix.
                        var prefixMatch = ModulePrefixes.FirstOrDefault(x => message.Content.StartsWith(Local.DeveloperPrefix + x));

                        IResult result;
                        if (prefixMatch != null)
                        {
                            //Use substring to remove the developer prefix as we cannot use the argPos here
                            var modifiedContent = message.Content.Substring(Local.DeveloperPrefix.Length).Replace(prefixMatch, $"{prefixMatch} ");
                            result = await CommandService.ExecuteAsync(context, modifiedContent, Provider);
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
            else if (BotConfig.UsePrefixSystem)
            {
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
                        Logger.Log(context.Message.Content, new LogContext(context));
                    }
                }
            }
            else
            {
                var discarded = 0;
                if (ModulePrefixes.Any(x => message.HasStringPrefix(x, ref discarded)))
                {
                    var context = new ShardedCommandContext(Client, message);

                    var prefixMatch = ModulePrefixes.FirstOrDefault(x => message.Content.StartsWith(x));
                    IResult result;
                    if (prefixMatch != null)
                    {
                        var modifiedContent = message.Content.Replace(prefixMatch, $"{prefixMatch} ");
                        result = await CommandService.ExecuteAsync(context, modifiedContent, Provider);
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
