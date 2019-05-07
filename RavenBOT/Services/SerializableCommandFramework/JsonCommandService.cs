using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Services.SerializableCommandFramework
{
    public class JsonCommandService
    {
        public static List<GuildMessageReplacementTypes.IContextReplacement> GuildMessageReplacementTypes { get; set; }

        public List<JsonNodeTree> Trees = new List<JsonNodeTree>();

        public class JsonNodeTree
        {
            public JsonNodeTree(string commandName, List<IJsonNode> nodes, string entryNodeId, ulong guildId, bool overrideCommand = true)
            {
                CommandName = commandName;
                Nodes = nodes;
                EntryNodeId = entryNodeId;
                GuildId = guildId;
                OverrideCommand = overrideCommand;
            }

            public string CommandName { get; set; }
            public List<IJsonNode> Nodes { get; set; }
            public string EntryNodeId { get; }
            public ulong GuildId { get; }
            public bool OverrideCommand { get; }

            public IJsonNode GetNode(string nodeId)
            {
                var nodeMatch = Nodes.FirstOrDefault(x => x.GetNodeId() == nodeId);
                return nodeMatch;
            }

            public class CommandServiceResponse
            {
                public CommandServiceResponse(bool overrideCommand, bool success)
                {
                    this.OverrideCommand = OverrideCommand;
                    this.Success = success;
                }

                public bool OverrideCommand { get; set; }
                public bool Success { get; set; }
            }

            public async Task<CommandServiceResponse> Execute(SocketCommandContext context, int argpos)
            {
                string command = context.Message.Content.Substring(argpos);
                if (!command.StartsWith(CommandName))
                {
                    return null;
                }

                //May be needed for something
                string commandArgs = command.Substring(CommandName.Length);

                GetNode(EntryNodeId)?.Execute(context);
                string nodeId = EntryNodeId;
                while (nodeId != null)
                {
                    nodeId = await GetNode(nodeId)?.Execute(context);
                }

                return new CommandServiceResponse(OverrideCommand, true);
            }

            public interface IJsonNode
            {
                string GetNodeId();

                //Returns the node ID of the following node.
                Task<string> Execute(SocketCommandContext context);
            }

            public class MessageSendActionNode : IJsonNode
            {

                private readonly string nextNodeId;
                private readonly string id;
                private readonly string messageToSend;

                public MessageSendActionNode(string id, string nextNodeId, string messageToSend /* Will require some data about what to actually execute*/)
                {
                    this.id = id;
                    this.nextNodeId = nextNodeId;
                    this.messageToSend = messageToSend;
                }

                public string GetNodeId()
                {
                    return id;
                }

                public async Task<string> Execute(SocketCommandContext context)
                {
                    await context.Channel.SendMessageAsync(messageToSend);
                    return nextNodeId;
                }
            }


            public class BooleanNode : IJsonNode
            {
                private readonly string id;

                private readonly string successNodeId;

                private readonly string failNodeId;

                private readonly string valueA;

                private readonly string valueB;

                private readonly ConditionExecution.ICondition condition;



                public BooleanNode(string id, string successNodeId, string failNodeId, ConditionExecution.ICondition condition, string valueA, string valueB)
                {
                    this.id = id;
                    this.successNodeId = successNodeId;
                    this.failNodeId = failNodeId;
                    this.condition = condition;
                    this.valueA = valueA;
                    this.valueB = valueB;
                }

                private bool TryPass(SocketCommandContext context)
                {
                    return condition.Condition(context, valueA, valueB);
                }

                public async Task<string> Execute(SocketCommandContext context)
                {
                    return TryPass(context) ? successNodeId : failNodeId;
                }

                public string GetNodeId()
                {
                    return id;
                }
            }

            public class EndNode : IJsonNode
            {
                public EndNode(string nodeId)
                {
                    this.NodeId = nodeId;
                }

                public string NodeId { get; set; }

                public string GetNodeId()
                {
                    return NodeId;
                }

                public Task<string> Execute(SocketCommandContext context)
                {
                    return null;
                }
            }
        }
    }
}
