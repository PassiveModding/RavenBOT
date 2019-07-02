using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Common;
using RavenBOT.Modules.Music.Methods;

namespace RavenBOT.Modules.Music.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class InAudioChannel : PreconditionBase
    {
        public bool PlayerCheck { get; }

        public InAudioChannel(bool playerCheck = false)
        {
            PlayerCheck = playerCheck;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var vicService = services.GetRequiredService<VictoriaService>();

            if (!vicService.IsConfigured())
            {
                return Task.FromResult(PreconditionResult.FromError("Voice commands have not been configured for this bot."));
            }

            var player = vicService.Client.GetPlayer(context.Guild.Id);

            if (context.User.GetVoiceChannel().Result is null)
                return Task.FromResult(PreconditionResult.FromError("You're not connected to a voice channel."));

            if (PlayerCheck && player is null)
                return Task.FromResult(PreconditionResult.FromError("There is no player created for this guild."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        public override string PreviewText() => $"Requires that the user is in an audio channel {(PlayerCheck ? "Also requires music to be playing" : null)}";

        public override string Name() => $"Require Audio Channel Precondition";
    }
}