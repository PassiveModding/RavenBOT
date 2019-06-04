using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Handlers;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Developer
{
    [RequireOwner]
    [Group("Developer")]
    public class Developer : ModuleBase<SocketCommandContext>
    {
        public LogHandler Logger { get; }
        public DeveloperSettings DeveloperSettings { get; }
        public IDatabase Database { get; }

        public Developer(LogHandler logger, IDatabase dbService, DeveloperSettings developerSettings)
        {
            Logger = logger;
            DeveloperSettings = developerSettings;
            Database = dbService;
        }

        /*
        [Command("AddModule")]
        public async Task AddModule([Remainder]string location)
        {
            try
            {
                var modules = await ModuleService.RegisterModule(location);
                foreach (var module in modules)
                {
                    await ReplyAsync($"Module Added: {module.Name}");
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }
        */

        [Command("EditHelpPreconditionSkips")]
        public async Task EditHelpPreconditionSkipsAsync(string skip)
        {
            var settings = DeveloperSettings.GetDeveloperSettings();
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

            DeveloperSettings.SetDeveloperSettings(settings);

            await ReplyAsync("Settings:\n" +
                             $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("ClearHelpPreconditionSkips")]
        public async Task ClearHelpPreconditionSkipsAsync()
        {
            var settings = DeveloperSettings.GetDeveloperSettings();
            settings.SkippableHelpPreconditions = new List<string>();
            DeveloperSettings.SetDeveloperSettings(settings);

            await ReplyAsync("Set.");
        }

        [Command("ViewHelpPreconditionSkips")]
        public async Task ViewHelpPreconditionSkipsAsync()
        {
            var settings = DeveloperSettings.GetDeveloperSettings();
            await ReplyAsync("Settings:\n" +
                             $"{string.Join("\n", settings.SkippableHelpPreconditions)}");
        }

        [Command("SetGame")]
        public async Task SetGame([Remainder]string game)
        {
            await Context.Client.SetActivityAsync(new Game(game));
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

        [Command("TestDatabase")]
        public async Task DBTest()
        {
            //Yes I know this is rudimentary and probably a terrible way of doing things but oh well.
            try
            {
                var testDoc = new DBTestDocument();

                testDoc.value = "Test";

                Database.Store(testDoc);
                Database.Store(testDoc, "Document");
                var docA = Database.Load<DBTestDocument>(null);
                var docB = Database.Load<DBTestDocument>("Document");
                var docList = Database.Query<DBTestDocument>();
                Database.RemoveDocument(testDoc);
                Database.Remove<DBTestDocument>("Document");
                Database.StoreMany(new List<DBTestDocument>(), x => x.value);
                await ReplyAsync("No Errors were thrown");
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString().FixLength(2047));
            }

        }

        public class DBTestDocument
        {
            public string value { get; set; }
        }
    }
}
