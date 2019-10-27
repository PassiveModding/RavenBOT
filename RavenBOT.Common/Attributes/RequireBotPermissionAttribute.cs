using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace RavenBOT.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RavenRequireBotPermissionAttribute : PreconditionBase
    {

        /// <summary>
        ///     Gets the specified <see cref="Discord.GuildPermission" /> of the precondition.
        /// </summary>
        public GuildPermission? GuildPermission { get; }
        /// <summary>
        ///     Gets the specified <see cref="Discord.ChannelPermission" /> of the precondition.
        /// </summary>
        public ChannelPermission? ChannelPermission { get; }

        /// <summary>
        ///     Requires the bot account to have a specific <see cref="Discord.GuildPermission"/>.
        /// </summary>
        /// <remarks>
        ///     This precondition will always fail if the command is being invoked in a <see cref="IPrivateChannel"/>.
        /// </remarks>
        /// <param name="permission">
        ///     The <see cref="Discord.GuildPermission"/> that the bot must have. Multiple permissions can be specified
        ///     by ORing the permissions together.
        /// </param>
        public RavenRequireBotPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
            ChannelPermission = null;
        }
        /// <summary>
        ///     Requires that the bot account to have a specific <see cref="Discord.ChannelPermission"/>.
        /// </summary>
        /// <param name="permission">
        ///     The <see cref="Discord.ChannelPermission"/> that the bot must have. Multiple permissions can be
        ///     specified by ORing the permissions together.
        /// </param>
        public RavenRequireBotPermissionAttribute(ChannelPermission permission)
        {
            ChannelPermission = permission;
            GuildPermission = null;
        }

        /// <inheritdoc />
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            IGuildUser guildUser = null;
            if (context.Guild != null)
                guildUser = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

            if (GuildPermission.HasValue)
            {
                if (guildUser == null)
                    return PreconditionResult.FromError("Command must be used in a guild channel.");
                if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                    return PreconditionResult.FromError($"Bot requires guild permission {GuildPermission.Value}.");
            }

            if (ChannelPermission.HasValue)
            {
                ChannelPermissions perms;
                if (context.Channel is IGuildChannel guildChannel)
                    perms = guildUser.GetPermissions(guildChannel);
                else
                    perms = ChannelPermissions.All(context.Channel);

                if (!perms.Has(ChannelPermission.Value))
                    return PreconditionResult.FromError($"Bot requires channel permission {ChannelPermission.Value}.");
            }

            return PreconditionResult.FromSuccess();
        }
        public override string Name() => $"Require Bot Permission Precondition";

        public override string PreviewText()
        {
            if (GuildPermission.HasValue)
            {
                return $"Requires that the bot has the {GuildPermission.Value} permissions for the server";
            }

            if (ChannelPermission.HasValue)
            {
                return $"Requires that the bot has the {ChannelPermission.Value} permissions for the channel";
            }

            throw new ArgumentNullException("There is no precondition permission specified for the provided implementation");
        }
    }
}