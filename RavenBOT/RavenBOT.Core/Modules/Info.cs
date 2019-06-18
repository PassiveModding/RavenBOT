﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RavenBOT.Common.Attributes.Preconditions;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Models;

namespace RavenBOT.Modules
{
    public class Main : InteractiveBase<ShardedCommandContext>
    {
        public CommandService CommandService { get; }
        public PrefixService PrefixService { get; }
        public HelpService HelpService { get; }
        public DiscordShardedClient Client { get; }
        public IServiceProvider Provider { get; }
        public DeveloperSettings DeveloperSettings { get; }
        public HttpClient HttpClient { get; }

        private Main(CommandService commandService, HttpClient http, PrefixService prefixService, DeveloperSettings devSettings, HelpService helpService, DiscordShardedClient client, IServiceProvider provider)
        {
            CommandService = commandService;
            PrefixService = prefixService;
            HelpService = helpService;
            Client = client;
            Provider = provider;
            DeveloperSettings = devSettings;
            HttpClient = http;
            Client.ShardReady += ShardReady;
        }

        public async Task ShardReady(DiscordSocketClient client)
        {
            await client.SetActivityAsync(new Game($"{PrefixService.GetPrefix(0)}help"));
        }

        [Command("Invite")]
        [Summary("Returns the bot invite")]
        public async Task InviteAsync()
        {
            await ReplyAsync("", false, $"Invite: https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591".QuickEmbed());
        }

        [Command("Help")]
        [Summary("Shows available commands based on the current user permissions")]
        public async Task HelpAsync()
        {
            await GenerateHelpAsync();
        }

        [RateLimit(1, 1, Measure.Minutes, RateLimitFlags.ApplyPerGuild)]
        [Command("Stats")]
        [Summary("Bot Info and Stats")]
        public async Task InformationAsync()
        {
            string changes;
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/PassiveModding/RavenBOT/commits");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                changes = "There was an error fetching the latest changes.";
            }
            else
            {
                dynamic result = JArray.Parse(await response.Content.ReadAsStringAsync());
                changes = $"[{((string)result[0].sha).Substring(0, 7)}]({result[0].html_url}) {result[0].commit.message}\n" + $"[{((string)result[1].sha).Substring(0, 7)}]({result[1].html_url}) {result[1].commit.message}\n" + $"[{((string)result[2].sha).Substring(0, 7)}]({result[2].html_url}) {result[2].commit.message}";
            }

            var embed = new EmbedBuilder();

            embed.WithAuthor(
                x =>
                {
                    x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                    x.Name = $"{Context.Client.CurrentUser.Username}'s Official Invite";
                    x.Url = $"https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591";
                });
            embed.AddField("Changes", changes.FixLength());

            embed.AddField("Members", $"Bot: {Context.Client.Guilds.Sum(x => x.Users.Count(z => z.IsBot))}\nHuman: {Context.Client.Guilds.Sum(x => x.Users.Count(z => !z.IsBot))}\nPresent: {Context.Client.Guilds.Sum(x => x.Users.Count(u => u.Status != UserStatus.Offline))}", true);
            embed.AddField("Members", $"Online: {Context.Client.Guilds.Sum(x => x.Users.Count(z => z.Status == UserStatus.Online))}\nAFK: {Context.Client.Guilds.Sum(x => x.Users.Count(z => z.Status == UserStatus.Idle))}\nDND: {Context.Client.Guilds.Sum(x => x.Users.Count(u => u.Status == UserStatus.DoNotDisturb))}", true);
            embed.AddField("Channels", $"Text: {Context.Client.Guilds.Sum(x => x.TextChannels.Count)}\nVoice: {Context.Client.Guilds.Sum(x => x.VoiceChannels.Count)}\nTotal: {Context.Client.Guilds.Sum(x => x.Channels.Count)}", true);
            embed.AddField("Guilds", $"Count: {Context.Client.Guilds.Count}\nTotal Users: {Context.Client.Guilds.Sum(x => x.MemberCount)}\nTotal Cached: {Context.Client.Guilds.Sum(x => x.Users.Count())}\n", true);
            var orderedShards = Context.Client.Shards.OrderByDescending(x => x.Guilds.Count).ToList();
            embed.AddField("Shards", $"Shards: {Context.Client.Shards.Count}\nMax: G:{orderedShards.First().Guilds.Count} ID:{orderedShards.First().ShardId}\nMin: G:{orderedShards.Last().Guilds.Count} ID:{orderedShards.Last().ShardId}", true);
            embed.AddField("Commands", $"Commands: {CommandService.Commands.Count()}\nAliases: {CommandService.Commands.Sum(x => x.Attributes.Count)}\nModules: {CommandService.Modules.Count()}", true);
            embed.AddField(":hammer_pick:", $"Heap: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\nUp: {GetUptime()}", true);
            embed.AddField(":beginner:", $"Written by: [PassiveModding](https://github.com/PassiveModding)\nDiscord.Net {DiscordConfig.Version}", true);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Shards")]
        [Summary("Displays information about all shards")]
        public async Task ShardInfoAsync()
        {
            var info = Context.Client.Shards.Select(x => $"[{x.ShardId}] {x.Status} {x.ConnectionState} - Guilds: {x.Guilds.Count} Users: {x.Guilds.Sum(g => g.MemberCount)}");
            await ReplyAsync($"```\n" + $"{string.Join("\n", info)}\n" + $"```");
        }

        [Command("FullHelp")]
        [Summary("Displays all commands without checking permissions")]
        public async Task FullHelpAsync()
        {
            await GenerateHelpAsync(false);
        }

        public async Task GenerateHelpAsync(bool checkPreconditions = true)
        {
            try
            {
                var res = await HelpService.PagedHelpAsync(Context, checkPreconditions);
                if (res != null)
                {
                    await PagedReplyAsync(res, new ReactionList
                    {
                        Backward = true,
                            First = false,
                            Forward = true,
                            Info = false,
                            Jump = true,
                            Last = false,
                            Trash = true
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private CommandInfo Command { get; set; }

        protected override void BeforeExecute(CommandInfo command)
        {
            Command = command;
            base.BeforeExecute(command);
        }

        private static string GetUptime()
        {
            return (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\D\ hh\H\ mm\M\ ss\S");
        }
    }
}