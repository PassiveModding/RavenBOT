using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Discord.Context.Interactive.Criteria;

namespace RavenBOT.Discord.Context.Interactive.Paginator
{
    internal class EnsureReactionFromSourceUserCriterion : ICriterion<SocketReaction>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
        {
            var ok = parameter.UserId == sourceContext.User.Id;
            return Task.FromResult(ok);
        }
    }
}