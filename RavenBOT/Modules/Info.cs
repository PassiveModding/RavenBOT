﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Preconditions;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules
{
    [Group("info.")]
    public class Info : InteractiveBase<ShardedCommandContext>
    {
        public CommandService CommandService { get; }
        public PrefixService PrefixService { get; }
        public HelpService HelpService { get; }
        public IServiceProvider Provider { get; }
        public DeveloperSettings DeveloperSettings { get; }
        public HttpClient HttpClient { get; }

        private Info(CommandService commandService, PrefixService prefixService, HelpService helpService, IServiceProvider provider)
        {
            CommandService = commandService;
            PrefixService = prefixService;
            HelpService = helpService;
            Provider = provider;
            DeveloperSettings = new DeveloperSettings(provider.GetRequiredService<IDatabase>());
            HttpClient = new HttpClient();
        }

        [Command("Invite")]
        public async Task InviteAsync()
        {
            await ReplyAsync($"Invite: https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
        }



        [Command("Help")]
        public async Task HelpAsync([Remainder]string moduleOrCommand = null)
        {
            await GenerateHelpAsync(moduleOrCommand);
        }

        [RateLimit(1, 1, Measure.Minutes, RateLimitFlags.ApplyPerGuild)]
        [Command("Stats")]
        [Summary("Bot Info and Stats")]
        public async Task InformationAsync()
        {
            string changes;

            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            using (var response = await HttpClient.GetAsync("https://api.github.com/repos/PassiveModding/RavenBOT/commits"))
            {
                if (!response.IsSuccessStatusCode)
                {
                    changes = "There was an error fetching the latest changes.";
                }
                else
                {
                    dynamic result = JArray.Parse(await response.Content.ReadAsStringAsync());
                    changes = $"[{((string)result[0].sha).Substring(0, 7)}]({result[0].html_url}) {result[0].commit.message}\n" + $"[{((string)result[1].sha).Substring(0, 7)}]({result[1].html_url}) {result[1].commit.message}\n" + $"[{((string)result[2].sha).Substring(0, 7)}]({result[2].html_url}) {result[2].commit.message}";
                }

                response.Dispose();
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

            embed.AddField("Members", $"Bot: {Context.Client.Guilds.Sum(x => x.Users.Count(z => z.IsBot))}\n" + $"Human: {Context.Client.Guilds.Sum(x => x.Users.Count(z => !z.IsBot))}\n" + $"Total: {Context.Client.Guilds.Sum(x => x.Users.Count)}", true);
            embed.AddField("Channels", $"Text: {Context.Client.Guilds.Sum(x => x.TextChannels.Count)}\n" + $"Voice: {Context.Client.Guilds.Sum(x => x.VoiceChannels.Count)}\n" + $"Total: {Context.Client.Guilds.Sum(x => x.Channels.Count)}", true);
            embed.AddField("Guilds", $"{Context.Client.Guilds.Count}", true);
            var orderedShards = Context.Client.Shards.OrderByDescending(x => x.Guilds.Count).ToList();
            embed.AddField("Stats", $"**Guilds:** {Context.Client.Guilds.Count}\n" + $"**Users:** {Context.Client.Guilds.Sum(x => x.MemberCount)}\n" + $"**Shards:** {Context.Client.Shards.Count}\n" + $"**Max Shard:** G:{orderedShards.First().Guilds.Count} ID:{orderedShards.First().ShardId}\n" + $"**Min Shard:** G:{orderedShards.Last().Guilds.Count} ID:{orderedShards.Last().ShardId}");

            embed.AddField(":hammer_pick:", $"Heap: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" + $"Up: {GetUptime()}", true);
            embed.AddField(":beginner:", "Written by: [PassiveModding](https://github.com/PassiveModding)\n" + $"Discord.Net {DiscordConfig.Version}", true);

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Shards")]
        [Summary("Displays information about all shards")]
        public async Task ShardInfoAsync()
        {
            var info = Context.Client.Shards.Select(x => $"[{x.ShardId}] {x.Status} - Guilds: {x.Guilds.Count} Users: {x.Guilds.Sum(g => g.MemberCount)}");
            await ReplyAsync($"```\n" + $"{string.Join("\n", info)}\n" + $"```");
        }

        [Command("FullHelp")]
        public async Task FullHelpAsync([Remainder][Summary("The name of a specific module or command")]string moduleOrCommand = null)
        {
            await GenerateHelpAsync(moduleOrCommand, false);
        }

        public async Task GenerateHelpAsync(string checkForMatch = null, bool checkPreconditions = true)
        {
            try
            {
                if (checkForMatch == null)
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
                else
                {
                    var res = await HelpService.ModuleCommandHelpAsync(Context, checkPreconditions, checkForMatch, Command);
                    if (res != null)
                    {
                        await InlineReactionReplyAsync(res);
                    }
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
