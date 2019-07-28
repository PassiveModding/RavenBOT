using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    //TODO: Potential different permissions for creating lobby
    [RavenRequireUserPermission(GuildPermission.Administrator)]
    public class LobbySetup : ELOBase
    {
        [Command("Create Lobby")]
        public async Task CreateLobbyAsync(int playersPerTeam = 5, Lobby.PickMode pickMode = Lobby.PickMode.Captains_RandomHighestRanked)
        {
            if (Context.Service.GetLobby(Context.Guild.Id, Context.Channel.Id) != null)
            {
                await ReplyAsync("This channel is already a lobby. Remove the lobby before trying top create a new one here.");
                return;
            }

            var lobby = Context.Service.CreateLobby(Context.Guild.Id, Context.Channel.Id);
            lobby.PlayersPerTeam = playersPerTeam;
            lobby.TeamPickMode = pickMode;
            Context.Service.Database.Store(lobby, Lobby.DocumentName(Context.Guild.Id, Context.Channel.Id));
            await ReplyAsync("New Lobby has been created\n" +
                $"Players per team: {playersPerTeam}\n" +
                $"Pick Mode: {pickMode}");
        }

        [Command("Set Player Count")]
        public async Task SetPlayerAsync(int playersPerTeam)
        {
            var lobby = Context.Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
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
            var lobby = Context.Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
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
            var pickDict = Extensions.ConvertEnumToDictionary<Lobby.PickMode>();
            await ReplyAsync($"{string.Join("\n", pickDict.Keys)}");
        }
    }
}