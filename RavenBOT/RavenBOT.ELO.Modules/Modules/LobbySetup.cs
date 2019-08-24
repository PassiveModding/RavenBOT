using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    //TODO: Potential different permissions for creating lobby
    [Preconditions.RequireAdmin]
    public class LobbySetup : InteractiveBase<ShardedCommandContext>
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
        [Alias("Set PickMode", "Set Pick Mode", "SetPickMode")]
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
        [Alias("Pick Modes")]
        public async Task DisplayPickModesAsync()
        {
            var pickDict = Extensions.ConvertEnumToDictionary<Lobby.PickMode>();
            await ReplyAsync($"{string.Join("\n", pickDict.Keys)}");
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