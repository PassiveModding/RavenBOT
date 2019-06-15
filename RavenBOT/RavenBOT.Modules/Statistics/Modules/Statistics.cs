using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Modules.Statistics.Methods;
using RavenBOT.Modules.Statistics.Models;

namespace RavenBOT.Modules.Statistics.Modules
{
    [Group("Stats")]
    [RequireOwner]
    //TODO: Test this module
    public class Statistics : InteractiveBase<ShardedCommandContext>
    {
        public Statistics(GraphManager graphManager, DBLManager dblManager, GrafanaManager grafanaManager)
        {
            GraphManager = graphManager;
            DblManager = dblManager;
            GrafanaManager = grafanaManager;
        }

        public GraphManager GraphManager { get; }
        public DBLManager DblManager { get; }
        public GrafanaManager GrafanaManager { get; }

        [Command("SetGraphiteUrl")]
        public async Task SetGraphiteUrl([Remainder] string url = null)
        {
            var config = GraphManager.GetConfig();
            config.GraphiteUrl = url;
            GraphManager.SaveConfig(config);
            await ReplyAsync("Url set, settings will apply after the next restart.");
        }

        [Command("SetDBLApiKey")]
        public async Task SetDBLAPIKey([Remainder] string key = null)
        {
            var config = DblManager.GetOrCreateConfig();
            config.APIKey = key;
            DblManager.SaveConfig(config);
            await ReplyAsync("API Key set.");
        }

        [Command("SetGrafanaUrl")]
        public async Task SetGrafanaUrl([Remainder] string url = null)
        {
            var config = GrafanaManager.GetGrafanaConfig();
            config.GrafanaUrl = url;
            GrafanaManager.SaveGrafanaConfig(config);
            await ReplyAsync("Url set.");
        }

        [Command("SetGrafanaApiKey")]
        public async Task SetGrafanaApiKey([Remainder] string key = null)
        {
            var config = GrafanaManager.GetGrafanaConfig();
            config.ApiKey = key;
            GrafanaManager.SaveGrafanaConfig(config);
            await ReplyAsync("Key set.");
        }

        [Command("RenderPanel")]
        public async Task RenderPanel(int panelId)
        {
            var config = GrafanaManager.GetGrafanaConfig();
            if (config.ApiKey != null && config.GrafanaUrl != null)
            {
                var organisationRequest = GrafanaManager.GetRequest(config, $"http://{config.GrafanaUrl}/api/org");
                var organisationContent = await GrafanaManager.GetHttpContentAsync(organisationRequest);
                var organisation = await GrafanaManager.RequestAndDeserializeAsync<GrafanaOrganisation>(organisationContent);

                var imageRequest = GrafanaManager.GetRequest(config, $"http://{config.GrafanaUrl}/render/dashboard-solo/db/stats?orgId={organisation.id}&panelId={panelId}&width=1000&height=500");
                var imageContent = await GrafanaManager.GetHttpContentAsync(imageRequest);
                var imageStream = await imageContent.ReadAsStreamAsync();
                await Context.Channel.SendFileAsync(imageStream, "graph.png");
            }
        }
    }
}