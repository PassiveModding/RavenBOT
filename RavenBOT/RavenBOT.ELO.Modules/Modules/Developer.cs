using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [Group("EloDev")]
    [RavenRequireOwner]
    public class Developer : ReactiveBase
    {
        public Developer(IDatabase database, Random random, ELOService service)
        {
            Database = database;
            Random = random;
            Service = service;
        }
        public IDatabase Database { get; }
        public Random Random { get; }
        public ELOService Service { get; }

        [Command("BanTest")]
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