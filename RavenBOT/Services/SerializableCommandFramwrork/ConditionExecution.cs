using Discord.Commands;

namespace RavenBOT.Services.SerializableCommandFramwrork
{
    public class ConditionExecution
    {
        public interface ICondition
        {
            bool Condition(SocketCommandContext context, string compareValue);

            string Name { get; set; }
        }

        public class ChannelIdEquals : ICondition
        {
            public bool Condition(SocketCommandContext context, string compareValue)
            {
                return context.Channel.Id.ToString().Equals(compareValue);
            }

            public string Name { get; set; } = "channelIdEquals";
        }
    }
}
