using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using RavenBOT.Modules.Automessage.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Automessage.Methods
{
    public class AutomessageHandler
    {
        public AutomessageHandler(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            Client.MessageReceived += MessageReceived;
        }

        private IDatabase Database { get; }
        private DiscordShardedClient Client { get; }
        
        public async Task MessageReceived(SocketMessage msg)
        {
            await Task.Yield();         
            if (!(msg is SocketUserMessage message))
            {
                return;
            }

            if (message.Channel is SocketTextChannel channel)
            {
                if (channel.Guild == null)
                {
                    return;
                }

                var config = Cache.GetOrAdd(channel.Id, key => new Lazy<AutomessageChannel>(() => GetAutomessageChannel(channel.Id)));
                if (config == null)
                {
                    return;
                }

                var messageChannel = config.Value;
                if (messageChannel == null)
                {
                    return;
                }

                messageChannel.MessageCount++;

                if (messageChannel.MessageCount >= messageChannel.RespondOn)
                {
                    messageChannel.MessageCount = 0;
                    
                    if (messageChannel.Response == null)
                    {
                        return;
                    }
                    channel.SendMessageAsync(messageChannel.Response);
                }
            }
        }

        public ConcurrentDictionary<ulong, Lazy<AutomessageChannel>> Cache = new ConcurrentDictionary<ulong, Lazy<AutomessageChannel>>();

        public AutomessageChannel GetAutomessageChannel(ulong channelId)
        {
            var config = Database.Load<AutomessageChannel>(AutomessageChannel.DocumentName(channelId));
            if (config == null)
            {
                return null;
            }
            return config;
        }

        public bool RemoveAutomessageChannel(AutomessageChannel channel)
        {
            Database.Remove<AutomessageChannel>(AutomessageChannel.DocumentName(channel.ChannelId));
            return Cache.TryRemove(channel.ChannelId, out _);
        }

        public void SaveAutomessageChannel(AutomessageChannel channel)
        {
            Database.Store(channel, AutomessageChannel.DocumentName(channel.ChannelId));
        }
    }
}