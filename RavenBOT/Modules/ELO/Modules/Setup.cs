using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Modules.ELO.Methods;
using RavenBOT.Modules.ELO.Models;

namespace RavenBOT.Modules.ELO.Modules
{
    [RequireContext(ContextType.Guild)]
    public class Setup : InteractiveBase<ShardedCommandContext>
    {
        public Setup(ELOService service)
        {
            Service = service;
        }

        public ELOService Service { get; }

        [Command("Create Competition")]
        public async Task CreateCompetitionAsync()
        {
            var comp = Service.CreateCompetition(Context.Guild.Id);
            await ReplyAsync("New competition created.");
        }

        [Command("Create Lobby")]
        public async Task CreateLobbyAsync(int playersPerTeam = 5, Lobby.PickMode pickMode = Lobby.PickMode.Captains)
        {
            if (Service.GetLobby(Context.Guild.Id, Context.Channel.Id) != null)
            {
                await ReplyAsync("This channel is already a lobby. Remove the lobby before trying top create a new one here.");
                return;
            }

            var lobby = Service.CreateLobby(Context.Guild.Id, Context.Channel.Id);
            lobby.PlayersPerTeam = playersPerTeam;
            lobby.TeamPickMode = pickMode;
            Service.Database.Store(lobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
            await ReplyAsync("New Lobby has been created\n" +
                            $"Players per team: {playersPerTeam}\n" +
                            $"Pick Mode: {pickMode}");
        }

        [Command("Set Player Count")]
        public async Task SetPlayerAsync(int playersPerTeam)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.PlayersPerTeam = playersPerTeam;
            await ReplyAsync($"There can now be up to {playersPerTeam} in each team.");
        }

        [Command("Set Pick Mode")]
        public async Task SetPickModeAsync(Lobby.PickMode pickMode)
        {
            var lobby = Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.TeamPickMode = pickMode;
            await ReplyAsync($"Pick mode set.");
        }

        [Command("PickModes")]
        public async Task DisplayPickModesAsync()
        {
            
        }
    }
}