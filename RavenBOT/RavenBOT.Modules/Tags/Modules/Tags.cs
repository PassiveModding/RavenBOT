using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Modules.Tags.Methods;

namespace RavenBOT.Modules.Tags.Modules
{
    [Group("Tags")]
    [RavenRequireContext(ContextType.Guild)]
    public class Tags : InteractiveBase<ShardedCommandContext>
    {
        public Tags(TagManager tagManager, HelpService helpService)
        {
            TagManager = tagManager;
            HelpService = helpService;
        }

        public TagManager TagManager { get; }
        public HelpService HelpService { get; }

        [Priority(100)]
        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "Tags"
            }, "This module allows you to set custom messages to be displayed with a command.");

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

        [Priority(100)]
        [Command("Add")]
        [Summary("Adds a new tag with the given name and message")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]    
        [Remarks("Requires administrator permissions")]
        public async Task AddTag([Summary("Wrap this in quotations if you want it to use spaces")] string name, [Remainder] string response)
        {
            var config = TagManager.GetTagGuild(Context.Guild.Id);

            if (config.Tags.Any(x => name.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                await ReplyAsync("There is already a tag with that name. Please delete it before trying to add a new one with that name.");
                return;
            }

            config.Tags.Add(new Models.TagGuild.Tag(Context.User.Id, name, response));
            TagManager.SaveTagGuild(config);
            await ReplyAsync("Tag Added.");
        }

        [Priority(100)]
        [Command("Remove")]
        [Summary("Removes the specified tag")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]    
        [Remarks("Requires administrator permissions")]
        public async Task RemoveTag([Remainder] string name)
        {
            var config = TagManager.GetTagGuild(Context.Guild.Id);

            var match = config.Tags.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (match == null)
            {
                await ReplyAsync("No tag found with that name.");
                return;
            }

            config.Tags.Remove(match);
            TagManager.SaveTagGuild(config);
            await ReplyAsync("Tag removed.");
        }

        [Command()]
        [Alias("Show")]
        [Summary("Shows all tags")]
        public async Task GetTag()
        {
            var config = TagManager.GetTagGuild(Context.Guild.Id);
            if (!config.Tags.Any())
            {
                await ReplyAsync("There are no tags.");
                return;
            }
            var list = config.Tags.Select(x => $"{x.Name}");
            await ReplyAsync(string.Join(", ", list).FixLength(2047));
        }

        [Command()]
        [Alias("Tag")]
        [Summary("Shows a tag with the given name")]
        public async Task GetTag([Remainder] string tagName)
        {
            var config = TagManager.GetTagGuild(Context.Guild.Id);
            var match = config.Tags.FirstOrDefault(x => x.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
            if (match == null)
            {
                await ReplyAsync("No tag found with that name.");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = $"Tag: {match.Name}".FixLength(64)
            };
            var creator = Context.Guild.GetUser(match.Creator);
            if (creator != null)
            {
                embed.Author = new EmbedAuthorBuilder
                {
                Name = creator.Nickname ?? creator.Username,
                IconUrl = creator.GetAvatarUrl()
                };
            }

            match.Hits++;

            embed.Footer = new EmbedFooterBuilder
            {
                Text = $"{match.Hits} Hits || Author: {creator?.ToString() ?? match.Creator.ToString()}"
            };

            embed.Description = match.Response.FixLength();

            await ReplyAsync("", false, embed.Build());
            TagManager.SaveTagGuild(config);
        }
    }
}