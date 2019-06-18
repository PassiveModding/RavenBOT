using System;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.ELO.Modules.Methods;

namespace RavenBOT.ELO.Modules.Bases
{
    public class ELOContext : ShardedCommandContext
    {
        public ELOService Service { get; }

        public ELOContext(DiscordShardedClient client, SocketUserMessage msg, IServiceProvider provider) : base(client, msg)
        {
            Service = provider.GetRequiredService<ELOService>();
        }
    }
}