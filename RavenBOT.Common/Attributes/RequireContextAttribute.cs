using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RavenRequireContextAttribute : PreconditionBase
    {
        /// <summary>
        ///     Gets the context required to execute the command.
        /// </summary>
        public ContextType Contexts { get; }

        /// <summary> Requires the command to be invoked in the specified context. </summary>
        /// <param name="contexts">The type of context the command can be invoked in. Multiple contexts can be specified by ORing the contexts together.</param>
        /// <example>
        /// <code language="cs">
        ///     [Command("secret")]
        ///     [RequireContext(ContextType.DM | ContextType.Group)]
        ///     public Task PrivateOnlyAsync()
        ///     {
        ///         return ReplyAsync("shh, this command is a secret");
        ///     }
        /// </code>
        /// </example>
        public RavenRequireContextAttribute(ContextType contexts)
        {
            Contexts = contexts;
        }

        /// <inheritdoc />
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            bool isValid = false;

            if ((Contexts & ContextType.Guild) != 0)
                isValid = context.Channel is IGuildChannel;
            if ((Contexts & ContextType.DM) != 0)
                isValid = isValid || context.Channel is IDMChannel;
            if ((Contexts & ContextType.Group) != 0)
                isValid = isValid || context.Channel is IGroupChannel;

            if (isValid)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError($"Invalid context for command; accepted contexts: {Contexts}."));
        }

        public override string PreviewText() => $"Restricts command usage to {Contexts}s";

        public override string Name() => $"Require Context Precondition";
    }
}