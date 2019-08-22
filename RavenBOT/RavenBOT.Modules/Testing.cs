using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using RavenBOT.Common;

namespace RavenBOT.Modules
{
    [RavenRequireOwner]
    [Group("Tests")]
    public class Testing : ModuleBase<SocketCommandContext>
    {
        public IServiceProvider Provider { get; }

        public Testing(IServiceProvider provider)
        {
            Provider = provider;
        }

        [Command("FileTest")]
        public async Task FileTest()
        {
            if (!Context.Message.Attachments.Any())
            {
                await ReplyAsync("No attachments.");
                return;
            }

            var file = Context.Message.Attachments.FirstOrDefault();
            var stream = await Provider.GetRequiredService<HttpClient>().GetStreamAsync(file.Url);
            await Context.Channel.SendFileAsync(stream, file.Filename, "As Stream");

            var bytes = await Provider.GetRequiredService<HttpClient>().GetByteArrayAsync(file.Url);
            await Context.Channel.SendFileAsync(new MemoryStream(bytes), file.Filename, "As Byte Array");
        }

        [Command("TestDownloadUsers")]
        public async Task TestDownload()
        {
            await ReplyAsync($"{Context.Guild.MemberCount}\n{Context.Guild.DownloadedMemberCount}\n{Context.Guild.Users.Count()}");
            await Context.Guild.DownloadUsersAsync();
            await ReplyAsync($"{Context.Guild.MemberCount}\n{Context.Guild.DownloadedMemberCount}\n{Context.Guild.Users.Count()}");
        }

        [Command("GetCommandJson")]
        public async Task GetCMDJson()
        {
            var overview = Provider.GetRequiredService<HelpService>().GetModuleOverviewJson();
            byte[] bytes = Encoding.ASCII.GetBytes(overview);
            await Context.Channel.SendFileAsync(new MemoryStream(bytes), "overview.json", "Overview file");
            Console.WriteLine();
        }

        [Command("TestAudioStream", RunMode = RunMode.Async)]
        [RavenRequireOwner]
        public async Task GetAudioStream(string filepath)
        {
            var channel = await Context.User.GetVoiceChannel();
            var audioClient = await channel.ConnectAsync();
            await ReplyAsync("Audio connected.");
            await SendAudioAsync(Context.Guild, audioClient, filepath);
            await ReplyAsync("Audio stream sent");
            await CheckStream();
        }

        [Command("CheckStream", RunMode = RunMode.Async)]
        [RavenRequireOwner]
        public async Task CheckStream()
        {
            var usr = (Context.User as SocketGuildUser).AudioStream;
            if (usr == null) return;

            using(MemoryStream ms = new MemoryStream())
            {
                int stopper = 0;
                var cancellation = new CancellationToken();
                while (usr.TryReadFrame(cancellation, out var frame))
                {
                    stopper++;
                    await ms.WriteAsync(frame.Payload, 0, frame.Payload.Length);
                }

                ms.Position = 0;

                var msArr = ms.ToArray();
                if (msArr.Length == 0)
                {
                    await ReplyAsync("Stream was empty");
                    return;
                }

                using(WaveFileWriter writer = new WaveFileWriter(Path.Combine(AppContext.BaseDirectory, "output.wav"), new WaveFormat()))
                {
                    writer.Write(msArr, 0, msArr.Length);
                }
            }
            await ReplyAsync("Finished");

        }

        public async Task SendAudioAsync(IGuild guild, IAudioClient audio, string path)
        {
            //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
            using(var ffmpeg = CreateProcess(path))
            using(var stream = audio.CreatePCMStream(AudioApplication.Music))
            {
                try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                finally { await stream.FlushAsync(); }
            }
        }

        private Process CreateProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
            });
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