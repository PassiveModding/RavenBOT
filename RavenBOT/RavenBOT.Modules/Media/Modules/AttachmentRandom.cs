using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Interfaces;
using RavenBOT.Extensions;
using RavenBOT.Modules.Media.Models;

namespace RavenBOT.Modules.Media.Modules
{
    [RavenRequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.AttachFiles)]
    [Group("Media Files")]
    //TODO: Moderator permissions instead of admin
    //TODO: Custom permissions to access files but mod to add/remove etc.
    public class AttachmentRandom : InteractiveBase<ShardedCommandContext>
    {
        public IDatabase Database { get; set; }

        public Random Random { get; set; }

        public HttpClient Client { get; set; }

        [Command("Add")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddFileAsync(string key)
        {
            if (!Context.Message.Attachments.Any(x => x.Size/1024/1024 <= 8))
            {
                await ReplyAsync("You must attach a file when using this command and ensure that it is less than 8mb in size.");
                return;
            }

            var config  = Database.Load<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));

            if (config == null)
            {
                config = new AttachmentRandomConfig(Context.Guild.Id, key);                
            }

            config.AttachmentUrls.AddRange(Context.Message.Attachments.Where(x => x.Size/1024/1024 <= 8).Select(x => x.Url).ToArray());
            Database.Store(config, AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));
            await ReplyAsync("Attachment(s) added.");
        }

        [Command("Clear")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveFilesAsync(string key)
        {
            if (!Context.Message.Attachments.Any(x => x.Size/1024/1024 <= 8))
            {
                await ReplyAsync("You must attach a file when using this command and ensure that it is less than 8mb in size.");
                return;
            }

            var configExists  = Database.Exists<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));

            if (configExists == false)
            {
                await ReplyAsync("Unknown key.");
                return;             
            }
            else
            {
                Database.Remove<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));
            }

            await ReplyAsync("Cleared Document.");
        }

        [Command("RemoveUrl")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task RemoveFilesAsync(string key, [Remainder]string url)
        {
            var config  = Database.Load<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));

            if (config == null)
            {
                await ReplyAsync("Key not found.");
                return;          
            }

            config.AttachmentUrls.RemoveAll(n => n.Equals(url, StringComparison.InvariantCultureIgnoreCase));
            if (config.AttachmentUrls.Count == 0)
            {
                Database.Remove<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));
                await ReplyAsync("Cleared Document.");
                return;
            }
            Database.Store(config, AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));
            await ReplyAsync("Removed attachment.");
        }

        [Command("ShowKeys")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ShowKeysAsync()
        {
            var config  = Database.Query<AttachmentRandomConfig>(x => x.GuildId == Context.Guild.Id).ToArray();

            if (!config.Any())
            {
                await ReplyAsync("None found.");
                return;          
            }

            await ReplyAsync("", false, string.Join("\n", config.Select(x => x.Key)).QuickEmbed());
        }

        [Command("ShowUrls")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ShowUrlsAsync(string key)
        {
            var config  = Database.Load<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));

            if (config == null)
            {
                await ReplyAsync("Key not found.");
                return;          
            }

            await ReplyAsync("", false, new EmbedBuilder()
            {
                Description = string.Join("\n", config.AttachmentUrls)
            }.Build());
        }

        [Command("Get")]
        [Alias("Image", "ImgGet", "ImageGet")]
        [RavenRequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task GetImageAsync(string key)
        {
            var config  = Database.Load<AttachmentRandomConfig>(AttachmentRandomConfig.DocumentName(Context.Guild.Id, key));

            if (config == null)
            {
                await ReplyAsync("Key not found.");
                return;          
            }

            var selection = config.AttachmentUrls.OrderBy(x => Random.Next()).FirstOrDefault();
            
            
            var response = await Client.GetAsync(selection);
            if (!response.IsSuccessStatusCode)
            {
                await ReplyAsync($"Error, attachment <{selection}> not found.");
                return;     
            }

            var contentDir = selection.LastIndexOf("\\");
            if (contentDir != -1)
            {
                selection = selection.Substring(contentDir);
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                await Context.Channel.SendFileAsync(stream, selection);
            }

            if (Context.Guild.CurrentUser.GuildPermissions.ManageMessages)
            {
                await Context.Message.DeleteAsync();
            }
        }
    }
}