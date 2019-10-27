using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace RavenBOT.Common
{
    public static partial class Extensions
    {        
        public static async Task<IMessage[]> GetFlattenedMessagesAsync(this ISocketMessageChannel channel, int count = 100)
        {
            var msgs = await channel.GetMessagesAsync(count).FlattenAsync();
            return msgs.ToArray();
        }
    }
}