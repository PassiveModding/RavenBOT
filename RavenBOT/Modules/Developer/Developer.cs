using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Handlers;
using RavenBOT.Models;
using RavenBOT.Modules.AutoMod.Models;
using RavenBOT.Modules.AutoMod.Models.Moderation;
using RavenBOT.Modules.Birthday.Models;
using RavenBOT.Modules.Captcha.Models;
using RavenBOT.Modules.Conversation.Models;
using RavenBOT.Modules.Events.Models.Events;
using RavenBOT.Modules.Games.Models;
using RavenBOT.Modules.Greetings.Models;
using RavenBOT.Modules.Levels.Models;
using RavenBOT.Modules.Media.Methods;
using RavenBOT.Modules.Moderator.Models;
using RavenBOT.Modules.Partner.Models;
using RavenBOT.Modules.Statistics.Models;
using RavenBOT.Modules.Tags.Models;
using RavenBOT.Modules.Tickets.Models;
using RavenBOT.Modules.Translation.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;
using static RavenBOT.Services.PrefixService;

namespace RavenBOT.Modules.Developer
{
    [RequireOwner]
    [Group("Developer")]
    public class Developer : ModuleBase<SocketCommandContext>
    {
        public LogHandler Logger { get; }
        public DeveloperSettings DeveloperSettings { get; }
        public IDatabase Database { get; }
        public GfycatManager GfyCat { get; }

        public Developer(LogHandler logger, IDatabase dbService, GfycatManager manager, DeveloperSettings developerSettings)
        {
            Logger = logger;
            DeveloperSettings = developerSettings;
            Database = dbService;
            GfyCat = manager;
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

        [Command("SetGfycatClient")]
        [Summary("Sets the gfycat client information")]
        public async Task SetGfycatClientAsync(string id, string secret)
        {
            var config = new GfycatManager.GfycatClientInfo
            {
                client_id = id,
                client_secret = secret
            };
            Database.Store(config, "GfycatClientInfo");
        }


        [Command("EmbedGfycatImage")]
        [Summary("Tests the getgfycaturl method")]
        public async Task EmbedTest([Remainder]string imageUrl)
        {
            var response = await GfyCat.GetGfyCatUrl(imageUrl);
            var embed = new EmbedBuilder()
            {
                Description = imageUrl,
                ImageUrl = response
            };
            await ReplyAsync("", false, embed.Build());
        }

        [Command("MigrateToLiteDB", RunMode = RunMode.Async)]
        public async Task MigrateDB()
        {
            var newDB = new LiteDataStore();
            newDB.StoreMany(Database.Query<LevelUser>().ToList(), x => LevelUser.DocumentName(x.UserId, x.GuildId));
            newDB.StoreMany(Database.Query<LevelConfig>().ToList(), x => LevelConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<ActionConfig>().ToList(), x => ActionConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<ActionConfig.ActionUser>().ToList(), x => ActionConfig.ActionUser.DocumentName(x.UserId, x.GuildId));
            newDB.StoreMany(Database.Query<BirthdayConfig>().ToList(), x => BirthdayConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<BirthdayModel>().ToList(), x => BirthdayModel.DocumentName(x.UserId));
            newDB.Store(Database.Load<BotConfig>("BotConfig"), "BotConfig");
            newDB.StoreMany(Database.Query<CaptchaConfig>().ToList(), x => CaptchaConfig.DocumentName(x.GuildId));
            newDB.Store(Database.Load<ConversationConfig>(ConversationConfig.DocumentName()), ConversationConfig.DocumentName());
            newDB.StoreMany(Database.Query<EventConfig>().ToList(), x => EventConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<GameServer>().ToList(), x => GameServer.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<GameUser>().ToList(), x => GameUser.DocumentName(x.UserId, x.GuildId));
            newDB.StoreMany(Database.Query<GoodbyeConfig>().ToList(), x => GoodbyeConfig.DocumentName(x.GuildId));            
            newDB.StoreMany(Database.Query<WelcomeConfig>().ToList(), x => WelcomeConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<ModerationConfig>().ToList(), x => ModerationConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<ModuleManagementService.ModuleConfig>().ToList(), x => ModuleManagementService.ModuleConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<PartnerConfig>().ToList(), x => PartnerConfig.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<Services.Licensing.LicenseService.QuantifiableLicense>().ToList(), x => x.Id);
            newDB.StoreMany(Database.Query<Services.Licensing.LicenseService.TimedLicense>().ToList(), x => x.Id);
            newDB.StoreMany(Database.Query<Services.Licensing.LicenseService.QuantifiableUserProfile>().ToList(), x => x.Id);
            newDB.StoreMany(Database.Query<Services.Licensing.LicenseService.TimedUserProfile>().ToList(), x => x.Id);
            newDB.StoreMany(Database.Query<Reminders.Models.Reminder>().ToList(), x => Reminders.Models.Reminder.DocumentName(x.UserId, x.ReminderNumber));
            newDB.StoreMany(Database.Query<TagGuild>().ToList(), x => TagGuild.DocumentName(x.GuildId));
            newDB.StoreMany(Database.Query<Ticket>().ToList(), x => Ticket.DocumentName(x.GuildId, x.TicketNumber));
            newDB.StoreMany(Database.Query<TranslateGuild>().ToList(), x => TranslateGuild.DocumentName(x.GuildId));
            newDB.Store(Database.Load<GraphiteConfig>(GrafanaConfig.DocumentName()), GrafanaConfig.DocumentName());
            newDB.Store(Database.Load<LoggerConfig>("LogConfig"), "LogConfig");
            newDB.Store(Database.Load<PerspectiveSetup>(PerspectiveSetup.DocumentName()), PerspectiveSetup.DocumentName());
            newDB.Store(Database.Load<PrefixInfo>("PrefixSetup"), "PrefixSetup");
            newDB.Store(Database.Load<DeveloperSettings.Settings>("Settings"), "Settings");
            newDB.Store(Database.Load<TimeTracker>("TimedModerations"), "TimedModerations");
            newDB.Store(Database.Load<TranslateConfig>("TranslateConfig"), "TranslateConfig");
            newDB.Store(Database.Load<GraphiteConfig>(GrafanaConfig.DocumentName()), GrafanaConfig.DocumentName());            
            await ReplyAsync("Done.");
        }

        public class DBTestDocument
        {
            public string value { get; set; }
        }
    }
}
