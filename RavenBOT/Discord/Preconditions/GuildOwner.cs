using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Discord.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class GuildOwner : PreconditionAttribute
    {
        /// <summary>
        ///     This will check wether or not a user has permissions to use a command/module
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <param name="services"></param>
        /// ///
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            //If the command is invoked in a DM channel we return success
            if (context.Channel is IDMChannel)
                return Task.FromResult(PreconditionResult.FromSuccess());

            //Check to see if the current user's ID matchs the guild owners 
            return Task.FromResult(context.Guild.OwnerId == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is not the Guild Owner!"));
        }
    }
}