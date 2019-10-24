using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RavenBOT.ELO.Modules.Models;

namespace RavenBOT.ELO.Modules.Methods
{
    public partial class ELOService 
    {
        public Timer CompetitionUpdateTimer { get; }
        public void UpdateCompetitionSetups(object stateInfo = null)
        {
            var _ = Task.Run(() =>
            {
                var competitions = Database.Query<CompetitionConfig>();
                var allPlayers = Database.Query<Player>().ToArray();
                foreach (var comp in competitions)
                {
                    var memberCount = allPlayers.Count(x => x.GuildId == comp.GuildId);
                    comp.RegistrationCount = memberCount;
                    Database.Store(comp, CompetitionConfig.DocumentName(comp.GuildId));
                }
            });
        }
    }
}