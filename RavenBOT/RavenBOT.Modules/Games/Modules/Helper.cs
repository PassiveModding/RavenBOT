using System.Threading.Tasks;
using Discord.Addons.Interactive;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Games.Modules
{
    [Group("Games")]
    public class Helper : InteractiveBase<ShardedCommandContext>
    {
        private HelpService HelpService { get; }

        public Helper(HelpService helpService)
        {
            HelpService = helpService;
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "games"
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