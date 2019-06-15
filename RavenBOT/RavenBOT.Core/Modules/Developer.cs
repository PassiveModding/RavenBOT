using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Handlers;
using RavenBOT.Models;
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
        public async Task SetGame([Remainder] string game)
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


        /*
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
        }*/
    }
}