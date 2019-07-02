using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RavenBOT.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RavenRequireOwnerAttribute : PreconditionBase
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                    if (context.User.Id != application.Owner.Id)
                        return PreconditionResult.FromError("Command can only be run by the owner of the bot.");
                    return PreconditionResult.FromSuccess();
                default:
                    return PreconditionResult.FromError($"{nameof(RequireOwnerAttribute)} is not supported by this {nameof(TokenType)}.");                    
            }
        }

        public override string Name() => $"Require Bot Owner Precondition";

        public override string PreviewText() => $"Requires that the command is executed by the account that created this bot.";
    }
}