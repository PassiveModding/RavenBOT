using System;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Preconditions
{
    public class IsRegistered : PreconditionBase
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

            var player = ec.Service.GetPlayer(context.Guild.Id, context.User.Id);

            if (player == null)
            {
                return Task.FromResult(PreconditionResult.FromError("You are not registered for this server."));
            }

            ec.CurrentPlayer = player;

            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        public override string Name()
        {
            return "IsRegistered";
        }

        public override string PreviewText()
        {
            return "Checks to see if the current user is registered";
        }
    }
}