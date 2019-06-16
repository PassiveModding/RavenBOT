using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Services;

namespace RavenBOT.Modules.RoleManagement.Modules
{
    [Group("RoleManager")]
    public class Helper : InteractiveBase<ShardedCommandContext>
    {
        private HelpService HelpService { get; }

        public Helper(HelpService helpService)
        {
            HelpService = helpService;
        }

        [Priority(100)]
        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "rolemanager"
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