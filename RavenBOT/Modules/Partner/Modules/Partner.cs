using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RavenBOT.Modules.Partner.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Partner.Modules
{
    [Group("Partner")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(Discord.GuildPermission.Administrator)]
    public class Partner : InteractiveBase<ShardedCommandContext>
    {
        public Partner(PartnerService partnerService)
        {
            Manager = partnerService;
        }

        public PartnerService Manager { get; }

        [Command("Toggle")]
        [Summary("Toggles the user of partner messages")]
        public async Task TogglePartnerAsync()
        {
            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.Enabled = !config.Enabled;
            Manager.SavePartnerConfig(config);
            await ReplyAsync($"Partner Enabled: {config.Enabled}");
        }

        [Command("SetChannel")]
        [Summary("Sets the channel to receive partner messages in")]
        public async Task SetChannelAsync()
        {
            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.ReceiverChannelId = Context.Channel.Id;
            Manager.SavePartnerConfig(config);
            await ReplyAsync($"Other server's partner messages will be sent in this channel.");
        }

        [Command("SetImage")]
        [Summary("Sets the partner image")]
        public async Task SetImageAsync([Remainder]string url = null)
        {
            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.ImageUrl = url;
            Manager.SavePartnerConfig(config);
            var embed = await config.GetEmbedAsync(Context.Guild);
            await ReplyAsync("Image Set", false, embed.Build());
        }

        [Command("ToggleThumbnail")]
        [Summary("Toggles the display of the server icon in partner messages")]
        public async Task ToggleThumbnailAsync()
        {
            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.UseThumb = !config.UseThumb;
            Manager.SavePartnerConfig(config);
            var embed = await config.GetEmbedAsync(Context.Guild);
            await ReplyAsync($"Show thumbnail: {config.UseThumb}", false, embed.Build());
        }

        [Command("SetColor")]
        [Alias("SetColour")]
        [Summary("Sets the color of your partner message")]
        public async Task SetColorAsync(int r, int g, int b)
        {
            if (r < 0 || r > 254 || g < 0 || g > 254 || b < 0 || b > 254)
            {
                await ReplyAsync("Color values muse each be in the range between 0-255");
                return;
            }

            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.Color = new Models.PartnerConfig.RGB(r, g, b);
            Manager.SavePartnerConfig(config);
            var embed = await config.GetEmbedAsync(Context.Guild);
            await ReplyAsync($"Color set.", false, embed.Build());
        }

        [Command("SetMessage")]
        [Summary("Sets the servers partner message")]
        public async Task SetMessageAsync([Remainder]string message)
        {
            if (message.Length > 512)
            {
                await ReplyAsync($"Partner message cannot be longer than 512 characters. Current Length = {message.Length}");
                return;
            }

            if (Context.Message.MentionedRoles.Any() || Context.Message.MentionedUsers.Any() || Context.Message.MentionedChannels.Any() || Context.Message.Content.Contains("@everyone") || Context.Message.Content.Contains("@here"))
            {
                await ReplyAsync("Partner Message cannot contain role or user mentions as they cannot be referenced from external guilds");
                return;
            }

            if (message.ToLower().Contains("discord.gg") || message.ToLower().Contains("discordapp.com") || message.ToLower().Contains("discord.me") || Regex.Match(message, @"(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?(d+i+s+c+o+r+d+|a+p+p)+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$").Success)
            {
                await ReplyAsync("No need to include an invite to the bot in your message. We will automatically generate one");
                return;
            }

            var config = Manager.GetOrCreatePartnerConfig(Context.Guild.Id);
            config.Message = message;
            Manager.SavePartnerConfig(config);
            var embed = await config.GetEmbedAsync(Context.Guild);
            await ReplyAsync("Message Set", false, embed.Build());
        }

        [Command("Trigger")]
        [RequireOwner]        
        [Summary("DEV: Triggers a partner event")]
        public async Task TriggerEvent()
        {
            await Manager.PartnerEvent();
            Console.WriteLine("Partner Trigger");
        }
    }
}