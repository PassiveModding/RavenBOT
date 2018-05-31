using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.Discord.Context.Interactive.Criteria
{
    public interface ICriterion<in T>
    {
        Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter);
    }
}