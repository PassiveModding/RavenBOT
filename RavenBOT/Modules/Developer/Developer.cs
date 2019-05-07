using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents;
using RavenBOT.Handlers;
using RavenBOT.Modules.Developer.Methods;
using RavenBOT.Services;

namespace RavenBOT.Modules.Developer
{
    [RequireOwner]
    [Group("Developer")]
    public class Developer : ModuleBase<SocketCommandContext>
    {
        public LogHandler Logger { get; }
        public IDocumentStore Store { get; }
        public Setup Setup { get; }

        public Developer(LogHandler logger, DatabaseService dbService)
        {
            Logger = logger;
            Store = dbService.GetStore();
            Setup = new Setup(Store);
        }

        [Command("EditHelpPreconditionSkips")]
        public async Task EditHelpPreconditionSkipsAsync(string skip)
        {
            var settings = Setup.GetDeveloperSettings();
            if (settings.SkippableHelpPreconditions.Contains(skip))
            {
                await ReplyAsync($"Removed {skip}");
                settings.SkippableHelpPreconditions.Remove(skip);
            }
            else
            {
                await ReplyAsync($"Added {skip}");
                settings.SkippableHelpPreconditions.Add(skip);
            }
            
            Setup.SetDeveloperSettings(settings);

            await ReplyAsync($"Settings:\n" +
                             $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("ClearHelpPreconditionSkips")]
        public async Task ClearHelpPreconditionSkipsAsync()
        {
            var settings = Setup.GetDeveloperSettings();
            settings.SkippableHelpPreconditions = new List<string>();
            Setup.SetDeveloperSettings(settings);

            await ReplyAsync($"Set.");
        }

        [Command("ViewHelpPreconditionSkips")]
        public async Task ViewHelpPreconditionSkipsAsync()
        {
            var settings = Setup.GetDeveloperSettings();
            await ReplyAsync($"Settings:\n" +
                             $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("SetLoggerChannel")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetLoggerChannelAsync(SocketTextChannel channel = null)
        {
            var originalConfig = Logger.GetLoggerConfig();
            if (channel == null)
            {
                await ReplyAsync("Removed logger channel");
                originalConfig.ChannelId = 0;
                originalConfig.GuildId = 0;
                originalConfig.LogToChannel = false;
                Logger.SetLoggerConfig(originalConfig);
                return;
            }

            originalConfig.GuildId = channel.Guild.Id;
            originalConfig.ChannelId = channel.Id;
            originalConfig.LogToChannel = true;
            Logger.SetLoggerConfig(originalConfig);
            await ReplyAsync($"Set Logger channel to {channel.Mention}");
        }
    }
}
