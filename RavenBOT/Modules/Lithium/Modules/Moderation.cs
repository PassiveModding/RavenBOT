using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Lithium.Methods;
using RavenBOT.Modules.Lithium.Models.Moderation;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Lithium.Modules
{
    [Group("lithium.moderation.")]
    public class Moderation : InteractiveBase<ShardedCommandContext>
    {
        public DiscordShardedClient Client { get; }
        public ModerationService ModerationService { get; }

        public Moderation(IDatabase database, DiscordShardedClient client)
        {
            Client = client;
            ModerationService = new ModerationService(database);
            Client.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            if (ModerationService.Perspective == null)
            {
                return;
            }

            if (!(msg is SocketUserMessage message))
            {
                return;
            }

            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }

            if (!(message.Channel is SocketTextChannel channel))
            {
                return;
            }

            await ModerationService.RunChecks(message, channel);
        }

        [Command("UseToxicity")]
        public async Task ToggleToxicityAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.UsePerspective = !config.UsePerspective;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"Toxic message checking: {config.UsePerspective}");
        }

        [Command("SetMaxToxicity")]
        public async Task SetMaxToxicityAsync(int max)
        {
            if (max > 50 && max < 100)
            {
                var config = ModerationService.GetModerationConfig(Context.Guild.Id);
                config.PerspectiveMax = max;
                ModerationService.SaveModerationConfig(config);
                await ReplyAsync($"Toxic Message Max Percentage: {config.PerspectiveMax}");
            }
            else
            {
                await ReplyAsync("Max value must be between 50 and 100");
            }
        }

        [Command("BlockMassMentions")]
        public async Task ToggleMentionsAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.BlockMassMentions = !config.BlockMassMentions;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Mass Mention Config: \n" +
                             $"Block Mass Mentions: {config.BlockMassMentions}\n" +
                             $"Channels Count: {config.MassMentionsIncludeChannels}\n" +
                             $"Users Count: {config.MassMentionsIncludeUsers}\n" +
                             $"Roles Count: {config.MassMentionsIncludeRoles}");
        }

        [Command("MassMentionRoles")]
        public async Task ToggleMentionRolesAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.MassMentionsIncludeRoles = !config.MassMentionsIncludeRoles;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Mass Mention Config: \n" +
                             $"Block Mass Mentions: {config.BlockMassMentions}\n" +
                             $"Channels Count: {config.MassMentionsIncludeChannels}\n" +
                             $"Users Count: {config.MassMentionsIncludeUsers}\n" +
                             $"Roles Count: {config.MassMentionsIncludeRoles}");
        }

        [Command("MassMentionUsers")]
        public async Task ToggleMentionUsersAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.MassMentionsIncludeUsers = !config.MassMentionsIncludeUsers;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Mass Mention Config: \n" +
                             $"Block Mass Mentions: {config.BlockMassMentions}\n" +
                             $"Channels Count: {config.MassMentionsIncludeChannels}\n" +
                             $"Users Count: {config.MassMentionsIncludeUsers}\n" +
                             $"Roles Count: {config.MassMentionsIncludeRoles}");
        }

        [Command("MassMentionChannels")]
        public async Task ToggleMentionChannelsAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.MassMentionsIncludeChannels = !config.MassMentionsIncludeChannels;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Mass Mention Config: \n" +
                             $"Block Mass Mentions: {config.BlockMassMentions}\n" +
                             $"Channels Count: {config.MassMentionsIncludeChannels}\n" +
                             $"Users Count: {config.MassMentionsIncludeUsers}\n" +
                             $"Roles Count: {config.MassMentionsIncludeRoles}");
        }

        [Command("BlockInvites")]
        public async Task ToggleInvitesAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.BlockInvites = !config.BlockInvites;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"Invite Blocking: {config.BlockInvites}");
        }

        [Command("BlockIps")]
        public async Task ToggleIpsAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.BlockIps = !config.BlockIps;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"IP Address Blocking: {config.BlockIps}");
        }

        [Command("UseBlacklist")]
        public async Task ToggleBlacklistAsync()
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.UseBlacklist = !config.UseBlacklist;
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync($"Using Blacklist: {config.UseBlacklist}");
        }

        [Command("Blacklist Add")]
        [Summary("Adds a word or message to the blacklist")]
        public async Task BlacklistAdd([Remainder]string message)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.BlacklistSimple.Add(new BlacklistSet.BlacklistMessage
            {
                Content = message,
                Regex = false
            });
            
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Added.");
        }

        [Command("Blacklist Regex Add")]
        [Summary("Adds a regex check to the blacklist")]
        public async Task BlacklistRegexAdd([Remainder]string message)
        {
            var config = ModerationService.GetModerationConfig(Context.Guild.Id);
            config.BlacklistSimple.Add(new BlacklistSet.BlacklistMessage
            {
                Content = message,
                Regex = true
            });
            
            ModerationService.SaveModerationConfig(config);
            await ReplyAsync("Added.");
        }

        [Command("SetToxicityToken")]
        public Task ToxicityToken(string token = null)
        {
            var setup = ModerationService.GetSetup();
            setup.PerspectiveToken = token;
            ModerationService.SetSetup(setup);
            return Task.CompletedTask;
        }
    }
}
