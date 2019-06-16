using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Services;

namespace RavenBOT.Modules.Moderator.Modules
{
    [Group("mod")]
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
                "mod"
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