using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Events.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Events.Modules
{
    [Group("events")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireContext(ContextType.Guild)]
    public class Events : InteractiveBase<ShardedCommandContext>
    {
        public EventService EventService { get; }

        public Events(EventService eventService)
        {
            EventService = eventService;
        }

        [Command("ToggleLogging")]
        [Summary("Toggles the logging of events in the current server")]
        public async Task ToggleLoggingAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.Enabled = !config.Enabled;
            EventService.SaveConfig(config);
            await ReplyAsync($"Logging Enabled: {config.Enabled}");
        }

        [Command("ChannelCreated")]
        [Summary("Toggles logging of channel creations")]
        public async Task ChannelCreatedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.ChannelCreated = !config.ChannelCreated;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log Channel Creations: {config.ChannelCreated}");
        }
        
                
        [Command("ChannelDeleted")]
        [Summary("Toggles logging of channel deletions")]
        public async Task ChannelDeletedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.ChannelDeleted = !config.ChannelDeleted;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log Channel Deletions: {config.ChannelDeleted}");
        }

        [Command("ChannelUpdated")]
        [Summary("Toggles logging of channel updates")]
        public async Task ChannelUpdatedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.ChannelUpdated = !config.ChannelUpdated;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log Channel Updates: {config.ChannelUpdated}");
        }

        [Command("UserUpdated")]
        [Summary("Toggles logging of user updates")]
        public async Task UserUpdatedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.UserUpdated = !config.UserUpdated;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log User Updates: {config.UserUpdated}");
        }

        [Command("UserJoined")]
        [Summary("Toggles logging of user joins")]
        public async Task UserJoinedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.UserJoined = !config.UserJoined;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log User Joins: {config.UserJoined}");
        }
        
        [Command("UserLeft")]
        [Summary("Toggles logging of user leaves")]
        public async Task UserLeftAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.UserLeft = !config.UserLeft;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log User Leaves: {config.UserLeft}");
        }  
        
        [Command("MessageUpdated")]        
        [Summary("Toggles logging of message updates")]
        public async Task MessageUpdatedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.MessageUpdated = !config.MessageUpdated;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log Message Updates: {config.MessageUpdated}");
        }

        [Command("MessageDeleted")]
        [Summary("Toggles logging of message deletions")]
        public async Task MessageDeletedAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.MessageDeleted = !config.MessageDeleted;
            EventService.SaveConfig(config);
            await ReplyAsync($"Log Message Deletes: {config.MessageDeleted}");
        }

        [Command("ShowSettings")]
        [Summary("Shows event log settings")]
        public async Task ShowSettingsAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            await ReplyAsync("**Event Log Config**\n" +
                             $"Channel Created: {config.ChannelCreated}\n" +
                             $"Channel Deleted: {config.ChannelDeleted}\n" +
                             $"Channel Updated: {config.ChannelUpdated}\n" +
                             $"Message Deleted: {config.MessageDeleted}\n" +
                             $"Message Updated: {config.MessageUpdated}\n" +
                             $"User Joined: {config.UserJoined}\n" +
                             $"User Left: {config.UserLeft}\n" +
                             $"User Updated: {config.UserUpdated}");
        }

        [Command("SetChannel")]
        public async Task SetChannelAsync()
        {
            var config = EventService.GetConfig(Context.Guild.Id);
            config.ChannelId = Context.Channel.Id;
            EventService.SaveConfig(config);
            await ReplyAsync($"Channel Set.");
        }
    }
}
