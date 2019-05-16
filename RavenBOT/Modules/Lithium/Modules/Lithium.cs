using System.Threading.Tasks;
using Discord.Addons.Interactive;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Lithium.Methods;
using RavenBOT.Modules.Lithium.Models.Moderation;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Lithium.Modules
{
    [Group("lithium.")]
    public class Lithium : InteractiveBase<ShardedCommandContext>
    {
        private HelpService HelpService { get; }

        public Lithium(HelpService helpService)
        {
            HelpService = helpService;
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "lithium."
            });

            if (res != null)
            {
                await PagedReplyAsync(res, new ReactionList
                {
                    Backward = true,
                    First = false,
                    Forward = true,
                    Info = false,
                    Jump = true,
                    Last = false,
                    Trash = true
                });
            }
            else
            {
                await ReplyAsync("N/A");
            }
        }
    }
}