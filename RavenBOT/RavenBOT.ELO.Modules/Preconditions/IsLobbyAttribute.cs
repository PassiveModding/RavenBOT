using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace RavenBOT.ELO.Modules.Preconditions
{
    public class IsLobby : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(context is ELOContext ec))
            {
                return Task.FromResult(PreconditionResult.FromError("Context is not an ELOContext."));
            }
            
            if (context.Guild == null)
            {
                return Task.FromResult(PreconditionResult.FromError("Invalid Command Context."));
            }

            var lobby = ec.Service.GetLobby(context.Guild.Id, context.User.Id);

            if (lobby == null)
            {
                return Task.FromResult(PreconditionResult.FromError("This channel is not a lobby."));
            }

            ec.CurrentLobby = lobby;

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}