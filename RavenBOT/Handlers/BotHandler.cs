﻿using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Models;

namespace RavenBOT.Handlers
{
    public class BotHandler
    {
        private ConfigModel Config { get; }
        private EventHandler Event { get; }
        private DiscordShardedClient Client { get; }
        public BotHandler(DiscordShardedClient client, EventHandler events, ConfigModel config)
        {
            Client = client;
            Event = events;
            Config = config;
        }

        public async Task InitializeAsync()
        {
            //These are our events, each time one of these is triggered it runs the corresponding method. Ie, the bot receives a message we run Event.MessageReceivedAsync
            Client.Log += Event.Log;
            Client.ShardReady += Event.ShardReady;
            Client.LeftGuild += Event.LeftGuild;
            Client.ShardConnected += Event.ShardConnected;
            Client.MessageReceived += Event.MessageReceivedAsync;

            //Here we log the bot in and start it. This MUST run for the bot to connect to discord.
            await Client.LoginAsync(TokenType.Bot, Config.Token);
            LogHandler.LogMessage("RavenBOT: Logged In");
            await Client.StartAsync();
            LogHandler.LogMessage("RavenBOT: Started");
        }
    }
}
