using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Discord.Context.Interactive.Criteria
{
    public class Criteria<T> : ICriterion<T>
    {
        private readonly List<ICriterion<T>> _critiera = new List<ICriterion<T>>();

        public async Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter)
        {
            foreach (var criterion in _critiera)
            {
                var result = await criterion.JudgeAsync(sourceContext, parameter).ConfigureAwait(false);
                if (!result) return false;
            }

            return true;
        }

        public Criteria<T> AddCriterion(ICriterion<T> criterion)
        {
            _critiera.Add(criterion);
            return this;
        }
    }
}