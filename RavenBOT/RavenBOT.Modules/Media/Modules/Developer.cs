using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common;
using RavenBOT.Modules.Media.Methods;

namespace RavenBOT.Modules.Media.Modules
{
    [Group("Media")]
    [RavenRequireOwner]
    public class Developer : InteractiveBase<ShardedCommandContext>
    {
        public IDatabase Database { get; }
        public GfycatManager GfyCat { get; }

        public Developer(IDatabase database, GfycatManager gfyCat)
        {
            Database = database;
            GfyCat = gfyCat;
        }

        [Command("SetGfycatClient")]
        [Summary("Sets the gfycat client information")]
        public async Task SetGfycatClientAsync(string id, string secret)
        {
            var config = new GfycatManager.GfycatClientInfo
            {
                client_id = id,
                client_secret = secret
            };
            Database.Store(config, "GfycatClientInfo");
        }

        [Command("EmbedGfycatImage")]
        [Summary("Tests the getgfycaturl method")]
        public async Task EmbedTest([Remainder] string imageUrl)
        {
            var response = await GfyCat.GetGfyCatUrl(imageUrl);
            var embed = new EmbedBuilder()
            {
                Description = imageUrl,
                ImageUrl = response
            };
            await ReplyAsync("", false, embed.Build());
        }
    }
}