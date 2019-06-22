using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Services;

namespace RavenBOT.Modules.Translation.Modules
{
    [Group("Translate")]
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
                "translate"
            }, $"You can follow a video tutorial for the translation module here: https://www.youtube.com/watch?v=CjHSXNurCMQ");

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