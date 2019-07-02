using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace RavenBOT.Common
{
    public class ShardChecker : IServiceable
    {
        public ShardChecker(DiscordShardedClient client)
        {
            client.ShardReady += ShardReadyAsync;
            Client = client;
        }

        public List<int> ReadyShardIds { get; set; } = new List<int>();
        public DiscordShardedClient Client { get; }

        public event Func<Task> AllShardsReady;

        public bool AllShardsReadyFired = false;

        public Task ShardReadyAsync(DiscordSocketClient socketClient)
        {
            if (AllShardsReadyFired)
            {
                return Task.CompletedTask;
            }

            if (!ReadyShardIds.Contains(socketClient.ShardId))
            {
                ReadyShardIds.Add(socketClient.ShardId);
            }

            if (Client.Shards.All(x => ReadyShardIds.Contains(x.ShardId)))
            {
                AllShardsReady.Invoke();
                AllShardsReadyFired = true;
            }

            return Task.CompletedTask;
        }
    }
}