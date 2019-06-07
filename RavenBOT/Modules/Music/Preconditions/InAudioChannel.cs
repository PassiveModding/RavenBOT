using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Extensions;
using RavenBOT.Modules.Music.Methods;

namespace RavenBOT.Modules.Music.Preconditions
{
    public class InAudioChannel : PreconditionAttribute
    {        
        public bool PlayerCheck { get; }

        public InAudioChannel(bool playerCheck = false)
        {
            PlayerCheck = playerCheck;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var vicService = services.GetRequiredService<VictoriaService>();
            var player = vicService.Client.GetPlayer(context.Guild.Id);

            if (context.User.GetVoiceChannel().Result is null)
                return Task.FromResult(PreconditionResult.FromError("You're not connected to a voice channel."));


            if (PlayerCheck && player is null)
                return Task.FromResult(PreconditionResult.FromError("There is no player created for this guild."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}