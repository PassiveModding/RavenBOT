namespace RavenBOT.Discord.Preconditions
{
    using System;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.Commands;

    /// <summary>
    /// A precondition to check for the guilds owner
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class GuildOwner : PreconditionAttribute
    {
        /// <summary>
        ///     This will check whether or not a user has permissions to use a command/module
        /// </summary>
        /// <param name="context">The Command Context</param>
        /// <param name="command">The command being invoked</param>
        /// <param name="services">The service provider</param>
        /// ///
        /// <returns>Success if the user is the owner of the current guild</returns>
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // If the command is invoked in a DM channel we return an error
            if (context.Channel is IDMChannel)
            {
                return Task.FromResult(PreconditionResult.FromError("User is not in a guild"));
            }

            // Override this permission check if the user is the bot owner. Helpful for assisting users that need help in their server
            if (context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            // Check to see if the current user's ID matches the guild owners 
            return Task.FromResult(context.Guild.OwnerId == context.User.Id ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is not the Guild Owner!"));
        }
    }
}