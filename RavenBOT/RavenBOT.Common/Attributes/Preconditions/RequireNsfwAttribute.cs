using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common.Attributes.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireNsfwAttribute : PreconditionBase
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel is ITextChannel text && text.IsNsfw)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("This command may only be invoked in an NSFW channel."));
        }

        public override string Name() => $"Require NSFW channel preconditon";

        public override string PreviewText() => $"Requires that the command is invoked in a NSFW channel";
    }
}