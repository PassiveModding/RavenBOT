using System.Collections.Generic;
using Discord.Commands;

namespace RavenBOT.Services.SerializableCommandFramework
{
    public class ConditionExecution
    {
        public interface ICondition
        {
            bool Condition(SocketCommandContext context, string valueA, string valueB);

            string Name { get; set; }
        }

        public class Comparator : ICondition
        {
            public string Name { get; set; } = "compareValues";

            public bool Condition(SocketCommandContext context, string valueA, string valueB)
            {
                valueA = GuildMessageReplacementTypes.DoReplacements(valueA, context);
                valueB = GuildMessageReplacementTypes.DoReplacements(valueB, context);

                return valueA.Equals(valueB);
            }
        }
    }
}
