using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.ELO.Modules.Bases;
using RavenBOT.ELO.Modules.Methods;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.Extensions;

namespace RavenBOT.ELO.Modules.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Setup : ELOBase
    {
        [Command("Create Lobby")]
        public async Task CreateLobbyAsync(int playersPerTeam = 5, Lobby.PickMode pickMode = Lobby.PickMode.Captains)
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
            var pickDict = StringExtensions.ConvertEnumToDictionary<Lobby.PickMode>();
            await ReplyAsync($"{string.Join("\n", pickDict.Keys)}");
        }

        [Command("SetCaptainMode")]
        public async Task SetCaptainModeAsync(Lobby.CaptainMode captainMode)
        {
            var lobby = Context.Service.GetLobby(Context.Guild.Id, Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Channel is not a lobby.");
                return;
            }

            lobby.CaptainSortMode = captainMode;
            await ReplyAsync($"Captain mode set.");
        }

        [Command("CaptainModes")]
        public async Task DisplayCaptainModesAsync()
        {
            var capDict = StringExtensions.ConvertEnumToDictionary<Lobby.CaptainMode>();
            await ReplyAsync($"{string.Join("\n", capDict.Keys)}");
        }

        [Command("SetRegisterRole")]
        public async Task SetRegisterRole(IRole role)
        {
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.RegisteredRankId = role.Id;
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Register role set.");
        }

        [Command("AddRank")]
        public async Task AddRank(IRole role, int points)
        {
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != role.Id).ToList();
            competition.Ranks.Add(new Rank
            {
                RoleId = role.Id,
                    Points = points
            });
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Rank added.");
        }

        [Command("AddRank")]
        public async Task AddRank(int points, IRole role)
        {
            await AddRank(role, points);
        }

        [Command("RemoveRank")]
        public async Task RemoveRank(ulong roleId)
        {
            var competition = Context.Service.GetCompetition(Context.Guild.Id) ?? Context.Service.CreateCompetition(Context.Guild.Id);
            competition.Ranks = competition.Ranks.Where(x => x.RoleId != roleId).ToList();
            Context.Service.Database.Store(competition, CompetitionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync("Rank Removed.");
        }

        [Command("RemoveRank")]
        public async Task RemoveRank(IRole role)
        {
            await RemoveRank(role.Id);
        }
    }
}