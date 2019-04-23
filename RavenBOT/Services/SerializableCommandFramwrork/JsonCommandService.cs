using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Services.SerializableCommandFramwrork
{
    public class JsonCommandService
    {
        public JsonCommandService()
        {
            GuildMessageReplacementTypes = new GuildMessageReplacementTypes().GetIGuildMessageReplacementTypes();
        }

        public static List<GuildMessageReplacementTypes.IGuildMessageReplacementType> GuildMessageReplacementTypes { get; set; }

        public List<JsonNodeTree> Trees = new List<JsonNodeTree>();

        public class JsonNodeTree
        {
            public JsonNodeTree(string commandName, List<IJsonNode> nodes, string entryNodeId, ulong guildId)
            {
                CommandName = commandName;
                Nodes = nodes;
                EntryNodeId = entryNodeId;
                GuildId = guildId;
            }

            public string CommandName { get; set; }
            public List<IJsonNode> Nodes { get; set; }
            public string EntryNodeId { get; }
            public ulong GuildId { get; }

            public IJsonNode GetNode(string nodeId)
            {
                var nodeMatch = Nodes.FirstOrDefault(x => x.GetNodeId() == nodeId);
                return nodeMatch;
            }
            
            public async Task Execute(SocketCommandContext context, int argpos)
            {
                string command = context.Message.Content.Substring(argpos);
                if (!command.StartsWith(CommandName))
                {
                    return;
                }

                //May be needed for something
                string commandArgs = command.Substring(CommandName.Length);

                GetNode(EntryNodeId)?.Execute(context);
                string nodeId = EntryNodeId;
                while (nodeId != null)
                {
                    nodeId = await GetNode(nodeId)?.Execute(context);
                }
            }

            public string DoReplacements(string message, SocketCommandContext context)
            {
                foreach (var messageReplacementType in GuildMessageReplacementTypes)
                {
                    if (message.Contains($"{{{messageReplacementType.Value}}}"))
                    {
                        message = message.Replace($"{{{messageReplacementType.Value}}}", messageReplacementType.ReplacementValue(context));
                    }
                }

                return message;
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

                private readonly string compareValue;

                private readonly ConditionExecution.ICondition condition;



                public BooleanNode(string id, string successNodeId, string failNodeId, ConditionExecution.ICondition condition, string compareValue = null)
                {
                    this.id = id;
                    this.successNodeId = successNodeId;
                    this.failNodeId = failNodeId;
                    this.condition = condition;
                    this.compareValue = compareValue;
                }

                private bool TryPass(SocketCommandContext context)
                {
                    return condition.Condition(context, compareValue);
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
