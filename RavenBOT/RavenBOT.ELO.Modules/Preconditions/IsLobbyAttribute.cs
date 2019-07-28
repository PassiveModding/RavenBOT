using System;
using System.Threading.Tasks;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;

namespace RavenBOT.ELO.Modules.Preconditions
{
    public class IsLobby : PreconditionBase
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

        public override string Name()
        {
            return "IsLobby";
        }

        public override string PreviewText()
        {
            return "Checks to see if the current channel is an ELO Lobby channel";
        }
    }
}