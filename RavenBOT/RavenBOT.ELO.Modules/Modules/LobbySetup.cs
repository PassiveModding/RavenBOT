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

        
        [Command("ReactOnJoinLeave", RunMode = RunMode.Sync)]
        public async Task ReactOnJoinLeaveAsync(bool react)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.ReactOnJoinLeave = react;
            Service.SaveLobby(lobby);
            await ReplyAsync($"React on join/leave set.");
        }

        [Command("PickModes")]
        //[Alias("Pick Modes")] ignore this as it can potentially conflict with the lobby Pick command.
        public async Task DisplayPickModesAsync()
        {
            await ReplyAsync(string.Join("\n", Extensions.EnumNames<Lobby.PickMode>()));
        }

        [Command("SetPickOrder", RunMode = RunMode.Sync)]
        [Alias("Set PickOrder", "Set Pick Order")]
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
            var res = "`PickOne` - Captains each alternate picking one player until there are none remaining\n" +
                    "`PickTwo` - 1-2-2-1-1... Pick order. Captain 1 gets first pick, then Captain 2 picks 2 players,\n"+
                    "then Captain 1 picks 2 players and then alternate picking 1 player until teams are filled\n" +
                    "This is often used to reduce any advantage given for picking the first player.";
            await ReplyAsync(res);
        }

        [Command("SetGameReadyAnnouncementChannel")]
        [Alias("GameReadyAnnouncementsChannel", "GameReadyAnnouncements", "ReadyAnnouncements", "SetReadyAnnouncements")]
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

            lobby.GameReadyAnnouncementChannel = destinationChannel.Id;
            Service.SaveLobby(lobby);
            await ReplyAsync($"Game ready announcements for the current lobby will be sent to {destinationChannel.Mention}");
        }

        [Command("SetGameResultAnnouncementChannel")]
        [Alias("SetGameResultAnnouncements", "GameResultAnnouncements")]
        public async Task GameResultAnnouncementChannel(SocketTextChannel destinationChannel = null)
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

            lobby.GameResultAnnouncementChannel = destinationChannel.Id;
            Service.SaveLobby(lobby);
            await ReplyAsync($"Game results for the current lobby will be sent to {destinationChannel.Mention}");
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
        
        [Command("MapMode", RunMode = RunMode.Sync)]
        public async Task MapModeAsync(MapSelector.MapMode mode)
        {
            if (mode == MapSelector.MapMode.Vote)
            {
                await ReplyAsync("That mode is not available currently.");
                return;
            }

            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            if (lobby.MapSelector == null)
            {
                lobby.MapSelector = new MapSelector();
            }
            
           

            lobby.MapSelector.Mode = mode;
            Service.SaveLobby(lobby);
            await ReplyAsync("Mode set.");
        }     

        [Command("MapMode", RunMode = RunMode.Async)]
        public async Task MapModeAsync()
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            if (lobby.MapSelector == null)
            {
                lobby.MapSelector = new MapSelector();
            }
            
            await ReplyAsync($"Current Map Mode: {lobby.MapSelector?.Mode}");
            return;
        }         

        [Command("MapModes")]
        public async Task MapModes()
        {
            await ReplyAsync(string.Join(", ", Extensions.EnumNames<MapSelector.MapMode>()));
        }

        [Command("ClearMaps")]
        public async Task MapClear()
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            if (lobby.MapSelector == null)
            {
                await ReplyAsync("There are no maps to clear.");
                return;
            }

            lobby.MapSelector.Maps.Clear();
            Service.SaveLobby(lobby);
            await ReplyAsync("Map added.");
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

            if (lobby.MapSelector == null)
            {
                lobby.MapSelector = new MapSelector();
            }

            lobby.MapSelector.Maps.Add(mapName);
            Service.SaveLobby(lobby);
            await ReplyAsync("Map added.");
        }

        [Command("AddMaps", RunMode = RunMode.Sync)]
        [Alias("Add Maps")]
        public async Task AddMapsAsync([Remainder]string commaSeparatedMapNames)
        {
            var mapNames = commaSeparatedMapNames.Split(',');
            if (!mapNames.Any()) return;

            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            if (lobby.MapSelector == null)
            {
                lobby.MapSelector = new MapSelector();
            }

            foreach (var map in mapNames)
            {
                lobby.MapSelector.Maps.Add(map);
            }
            Service.SaveLobby(lobby);
            await ReplyAsync("Map(s) added.");
        }

        [Command("DelMap", RunMode = RunMode.Sync)]
        public async Task RemoveMapAsync([Remainder]string mapName)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby.");
                return;
            }

            if (lobby.MapSelector == null)
            {
                await ReplyAsync("There are no maps to remove.");
                return;
            }

            if (lobby.MapSelector.Maps.Remove(mapName))
            {
                Service.SaveLobby(lobby);
                await ReplyAsync("Map removed.");
            }
            else
            {
                await ReplyAsync("There was no map matching that name found.");
            }
        }

        [Command("ToggleDms")]
        public async Task ToggleDmsAsync()
        {
            if (!Context.Channel.IsLobby(Service, out var lobby)) return;

            lobby.DmUsersOnGameReady = !lobby.DmUsersOnGameReady;
            Service.SaveLobby(lobby);
            await ReplyAsync($"DM when games are ready: {lobby.DmUsersOnGameReady}");
        }
    }
}