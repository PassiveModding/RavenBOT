using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace RavenBOT.Discord.Context.Interactive.Criteria
{
    public class EnsureSourceChannelCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            var ok = sourceContext.Channel.Id == parameter.Channel.Id;
            return Task.FromResult(ok);
        }
    }
}