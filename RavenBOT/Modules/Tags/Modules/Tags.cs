using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Extensions;
using RavenBOT.Modules.Tags.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Tags.Modules
{
    [Group("Tags")]
    [RequireContext(ContextType.Guild)]
    public class Tags : InteractiveBase<ShardedCommandContext>
    {
        public Tags(TagManager tagManager)
        {
            TagManager = tagManager;
        }

        public TagManager TagManager { get; }

        [Command("Add")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddTag(string name, [Remainder]string response)
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

        [Command("Remove")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveTag([Remainder]string name)
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

        [Command("Show")]
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

        [Command("Tag")]
        public async Task GetTag([Remainder]string tagName)
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