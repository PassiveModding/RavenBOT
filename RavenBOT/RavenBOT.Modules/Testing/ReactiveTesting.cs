using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RavenBOT.Common;

namespace RavenBOT.Modules.Testing
{
    [RavenRequireOwner]
    public class ReactiveTesting : ReactiveBase
    {
        [Command("ReactiveTest")]
        public async Task PagerTest()
        {
            var pages = new List<ReactivePage>
            {
                new ReactivePage
                {
                    Description = "a"
                },new ReactivePage
                {
                    Description = "b"
                },new ReactivePage
                {
                    Description = "c"
                },new ReactivePage
                {
                    Description = "d"
                },new ReactivePage
                {
                    Description = "e"
                },new ReactivePage
                {
                    Description = "f"
                },
            };
            var pager = new ReactivePager(pages);

            await PagedReplyAsync(pager
            .ToCallBack()
            .WithDefaultPagerCallbacks()
            .WithCallback(new Emoji("➕"), async (callback, reaction) =>
            {
                callback.AddPage(new ReactivePage
                {
                    Description = callback.pages.ToString()
                });
                await callback.RenderAsync();
                return false;
            })
            .WithCallback(new Emoji("➖"), async (callback, reaction) =>
            {
                callback.RemovePage(callback.page);
                await callback.RenderAsync();
                return false;
            })
            .WithCallback(new Emoji("🔄"), async (callback, reaction) =>
            {
                callback.SetPages(pages);
                await callback.RenderAsync();
                return false;
            }));
        }
    }
}