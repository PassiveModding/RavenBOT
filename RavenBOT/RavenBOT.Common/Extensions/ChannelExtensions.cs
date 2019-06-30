using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RavenBOT.Common.Extensions
{
    public static class ChannelExtensions
    {        
        public static async Task<IMessage[]> GetFlattenedMessagesAsync(this ISocketMessageChannel channel, int count = 100)
        {
            var msgs = await channel.GetMessagesAsync(count).FlattenAsync();
            return msgs.ToArray();
        }
    }
}