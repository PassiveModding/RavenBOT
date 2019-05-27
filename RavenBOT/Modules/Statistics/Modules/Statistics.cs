using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Statistics.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Statistics.Modules
{
    [Group("Stats")]
    [RequireOwner]
    //TODO: Test this module
    public class Statistics : InteractiveBase<ShardedCommandContext>
    {
        public Statistics(IDatabase database, DiscordShardedClient client)
        {
            GraphManager = new GraphManager(database, client);
        }

        public GraphManager GraphManager { get; }

        [Command("SetUrl")]
        public async Task SetGraphiteUrl([Remainder]string url = null)
        {
            var config = GraphManager.GetConfig();
            config.GraphiteUrl = url;
            GraphManager.SaveConfig(config);
            await ReplyAsync("Url set, settings will apply after the next restart.");
        }
    }
}