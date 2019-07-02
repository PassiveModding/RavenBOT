using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Common;
using RavenBOT.Modules.Moderator.Methods;

namespace RavenBOT.Modules.Moderator.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class Moderator : PreconditionBase
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var modHandler = services.GetRequiredService<ModerationHandler>();

            if (context.Guild == null)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a guild only command."));
            }

            var user = ((SocketGuildUser) context.User);

            //Always allow admins the moderator permission
            if (user.GuildPermissions.Administrator)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            var config = modHandler.GetModeratorConfig(context.Guild.Id);
            if (config != null && config.ModeratorRoles.Any())
            {
                //Only check mod roles if there are actually some configured.
                if (user.Roles.Any(x => config.ModeratorRoles.Contains(x.Id)))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            //As the user is not an admin and there are no mod roles configures, return an error.
            return Task.FromResult(PreconditionResult.FromError("You must be an administrator or moderator to run this command."));
        }

        public override string Name() => $"Require Moderator Precondition";

        public override string PreviewText() => $"Requires the user has a moderator role or administrator permissions";
    }
}