using System;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Preconditions
{
    public class IsRegistered : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(context is ELOContext ec))
            {
                return Task.FromResult(PreconditionResult.FromError("Invalid Context."));
            }
            
            if (context.Guild == null)
            {
                return Task.FromResult(PreconditionResult.FromError("Invalid Command Context."));
            }

            if (ec.Service.GetPlayer(context.Guild.Id, context.User.Id) == null)
            {
                return Task.FromResult(PreconditionResult.FromError("You are not registered for this server."));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}