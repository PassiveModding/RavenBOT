using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Methods.Migrations;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.ELO.Modules.Premium;

namespace RavenBOT.ELO.Modules.Modules
{
    [Group("EloDev")]
    [RavenRequireOwner]
    public class Developer : ReactiveBase
    {
        public Developer(IDatabase database, Random random, ELOService service, PatreonIntegration prem, ELOMigrator migrator)
        {
            Database = database;
            Random = random;
            Service = service;
            PremiumService = prem;
            Migrator = migrator;
        }
        public IDatabase Database { get; }
        public Random Random { get; }
        public ELOService Service { get; }
        public PatreonIntegration PremiumService { get; }
        public ELOMigrator Migrator { get; }

        [Command("RunMigrationTask", RunMode = RunMode.Sync)]
        public async Task RunMigrationTaskAsync()
        {
            await ReplyAsync("Running migration.");
            var _ = Task.Run(async () => 
            {
                Migrator.RunMigration();
                await ReplyAsync("Done.");
            });
        }

        [Command("AddPremiumRole", RunMode = RunMode.Sync)]
        public async Task AddRoleAsync(SocketRole role, int maxCount)
        {
            var config = PremiumService.GetConfig();
            config.GuildId = Context.Guild.Id;
            config.Roles.Add(role.Id, new PatreonIntegration.PatreonConfig.ELORole
            {
                RoleId = role.Id,
                MaxRegistrationCount = maxCount
            });
            PremiumService.SaveConfig(config);
            await ReplyAsync("Done.");
        }

        [Command("SetRegistrationCounts", RunMode = RunMode.Async)]
        public async Task SetCounts()
        {
            Service.UpdateCompetitionSetups();
            await ReplyAsync("Running... This will not send a message upon completion.");
        }


        [Command("LastLegacyPremium", RunMode = RunMode.Async)]
        public async Task LastLegacyPremium()
        {
            var date = Migrator.Legacy.GetLatestExpiryDate();
            await ReplyAsync($"Expires on: {date.ToString("dd MMM yyyy")} {date.ToShortTimeString()}\nRemaining: {(date - DateTime.UtcNow).GetReadableLength()}");
        }

        [Command("TogglePremium", RunMode = RunMode.Async)]
        public async Task TogglePremium()
        {
            var config = PremiumService.GetConfig();
            config.Enabled = !config.Enabled;
            PremiumService.SaveConfig(config);
            await ReplyAsync($"Premium Enabled: {config.Enabled}");
        }

        [Command("SetPatreonUrl", RunMode = RunMode.Async)]
        public async Task SetPatreonUrl([Remainder]string url)
        {
            var config = PremiumService.GetConfig();
            config.PageUrl = url;
            PremiumService.SaveConfig(config);
            await ReplyAsync($"Set.");
        }

        [Command("SetPatreonGuildInvite", RunMode = RunMode.Async)]
        public async Task SetPatreonGuildInvite([Remainder]string url)
        {
            var config = PremiumService.GetConfig();
            config.ServerInvite = url;
            PremiumService.SaveConfig(config);
            await ReplyAsync($"Set.");
        }

        [Command("BanTest", RunMode = RunMode.Sync)]
        public async Task BanTest()
        {
            if (!Context.User.IsRegistered(Service, out var player))
            {
                return;
            }

            player.BanHistory.Add(new Player.Ban(TimeSpan.FromHours(1), Context.User.Id, "Test"));
            player.CurrentBan.Comment = "ok";
            await ReplyAsync("Done");
        }

        [Command("RandomMap")]
        public async Task RndMap(bool history)
        {
            if (!Context.Channel.IsLobby(Service, out var lobby))
            {
                return;
            }

            if (lobby.MapSelector == null) return;

            await ReplyAsync(lobby.MapSelector.RandomMap(Random, history) ?? "N/A");
            Service.SaveLobby(lobby);
        }

        [Command("ClearAllQueues", RunMode = RunMode.Sync)]
        public async Task ClearAllLobbies()
        {
            var lobbies = Database.Query<Lobby>().ToList();
            foreach (var lobby in lobbies)
            {
                lobby.Queue.Clear();
            }
            Database.StoreMany<Lobby>(lobbies, x => Lobby.DocumentName(x.GuildId, x.ChannelId));
            await ReplyAsync("Cleared all queues");
        }

        [Command("FixPlayerNames", RunMode = RunMode.Sync)]
        public async Task FixNamesAsync()
        {
            var players = Database.Query<Player>().ToList();
            var toRemove = new List<ulong>();
            foreach (var player in players)
            {
                var user = Context.Client.GetUser(player.UserId);
                if (user == null)
                {
                    toRemove.Add(player.UserId);
                    continue;
                }
                player.DisplayName = user.Username;
            }
            Database.RemoveMany<Player>(players.Where(x => toRemove.Contains(x.UserId)).Select(x => Player.DocumentName(x.GuildId, x.UserId)).ToList());
            Database.StoreMany<Player>(players.Where(x => !toRemove.Contains(x.UserId)).ToList(), x => Player.DocumentName(x.GuildId, x.UserId));
            await ReplyAsync("All usernames have been reset to the user's discord username.");
        }
    }
}