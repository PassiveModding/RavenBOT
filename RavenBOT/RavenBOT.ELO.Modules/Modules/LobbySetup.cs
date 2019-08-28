using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    //TODO: Potential different permissions for creating lobby
    [Preconditions.RequireAdmin]
    public class LobbySetup : ReactiveBase
    {
        public ELOService Service { get; }

        public LobbySetup(ELOService service)
        {
            Service = service;
        }

        [Command("CreateLobby", RunMode = RunMode.Sync)]
        [Alias("Create Lobby")]
        public async Task CreateLobbyAsync(int playersPerTeam = 5, Lobby.PickMode pickMode = Lobby.PickMode.Captains_RandomHighestRanked)
        {
            if (Service.GetLobby(Context.Guild.Id, Context.Channel.Id) != null)
            {
                await ReplyAsync("This channel is already a lobby. Remove the lobby before trying top create a new one here.");
                return;
            }

            var lobby = Service.CreateLobby(Context.Guild.Id, Context.Channel.Id);
            lobby.PlayersPerTeam = playersPerTeam;
            lobby.TeamPickMode = pickMode;
            Service.SaveLobby(lobby);
            await ReplyAsync("New Lobby has been created\n" +
                $"Players per team: {playersPerTeam}\n" +
                $"Pick Mode: {pickMode}");
        }

        [Command("SetPlayerCount", RunMode = RunMode.Sync)]
        [Alias("Set Player Count", "Set PlayerCount")]
        public async Task SetPlayerAsync(int playersPerTeam)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.PlayersPerTeam = playersPerTeam;
            Service.SaveLobby(lobby);
            await ReplyAsync($"There can now be up to {playersPerTeam} in each team.");
        }

        [Command("SetPickMode", RunMode = RunMode.Sync)]
        [Alias("Set PickMode", "Set Pick Mode")]
        public async Task SetPickModeAsync(Lobby.PickMode pickMode)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.TeamPickMode = pickMode;
            Service.SaveLobby(lobby);
            await ReplyAsync($"Pick mode set.");
        }

        [Command("PickModes")]
        //[Alias("Pick Modes")] ignore this as it can potentially conflict with the lobby Pick command.
        public async Task DisplayPickModesAsync()
        {
            var pickDict = Extensions.ConvertEnumToDictionary<Lobby.PickMode>();
            await ReplyAsync($"{string.Join("\n", pickDict.Keys)}");
        }

        [Command("SetPickOrder", RunMode = RunMode.Sync)]
        [Alias("Set PickOrder", "Set PickOrder")]
        public async Task SetPickOrderAsync(GameResult.CaptainPickOrder orderMode)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.CaptainPickOrder = orderMode;
            Service.SaveLobby(lobby);
            await ReplyAsync($"Captain pick order set.");
        }


        [Command("PickOrders")]
        public async Task DisplayPickOrdersAsync()
        {
            var pickDict = Extensions.ConvertEnumToDictionary<GameResult.CaptainPickOrder>();
            await ReplyAsync($"{string.Join("\n", pickDict.Keys)}");
        }

        [Command("SetGameReadyAnnouncementChannel")]
        public async Task GameReadyAnnouncementChannel(SocketTextChannel destinationChannel = null)
        {
            if (destinationChannel == null)
            {
                await ReplyAsync("You need to specify a channel for the announcements to be sent to.");
                return;
            }

            if (destinationChannel.Id == Context.Channel.Id)
            {
                await ReplyAsync("You cannot send announcements to the current channel.");
                return;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            lobby.GameReadyAnnouncementChannel = Context.Channel.Id;
            Service.SaveLobby(lobby);
            await ReplyAsync($"Game ready announcements for the current lobby will be sent to {destinationChannel.Mention}");
        }

        [Command("SetMinimumPoints", RunMode = RunMode.Sync)]
        public async Task SetMinimumPointsAsync(int points)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            lobby.MinimumPoints = points;

            Service.SaveLobby(lobby);
            await ReplyAsync($"Minimum points is now set to {points}.");
        }

        [Command("ResetMinimumPoints", RunMode = RunMode.Sync)]
        public async Task ResetMinPointsAsync()
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            lobby.MinimumPoints = null;

            Service.SaveLobby(lobby);
            await ReplyAsync($"Minimum points is now disabled for this lobby.");
        }
        
        [Command("AddMap", RunMode = RunMode.Sync)]
        [Alias("Add Map")]
        public async Task AddMapAsync([Remainder]string mapName)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            lobby.Maps.Add(mapName);
            lobby.Maps = lobby.Maps.Distinct().ToHashSet();
            Service.SaveLobby(lobby);
            await ReplyAsync("Map added.");
        }
    }
}