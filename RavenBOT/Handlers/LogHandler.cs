using System;
using Discord;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Handlers
{
    public class LogHandler
    {
        private DiscordShardedClient Client { get; }

        private IDatabase Store { get; }

        private LoggerConfig Config { get; }

        public LoggerConfig GetLoggerConfig()
        {
            return Config;
        }

        public void SetLoggerConfig(LoggerConfig newConfig)
        {
            Store.Store(newConfig, "LogConfig");
        }

        public LogHandler(DiscordShardedClient client, IDatabase store)
        {
            Client = client;
            Store = store;

            Config = Store.Load<LoggerConfig>("LogConfig");
            if (Config == null)
            {
                Config = new LoggerConfig();
                Store.Store(Config, "LogConfig");
            }
        }

        private Color LogSeverityAsColor(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Info:
                    return Color.Blue;
                case LogSeverity.Critical:
                    return Color.DarkRed;
                case LogSeverity.Error:
                    return Color.Red;
                case LogSeverity.Warning:
                    return Color.Gold;
                case LogSeverity.Debug:
                    return Color.DarkerGrey;
                case LogSeverity.Verbose:
                    return Color.Green;
                default:
                    return Color.Purple;
            }
        }

        private string MakeLogMessage(string message, LogSeverity severity, LogContext context = null)
        {
            var newSev = severity.ToString().PadRight(20).Substring(0, 4).ToUpper();
            var contextString = "";
            if (context != null)
            {
                contextString = $"\n[U:{context.userId} G:{context.guildId} C:{context.channelId} M:{context.message}]";
            }
            return $"[{newSev}][{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToShortTimeString()}] {message}{contextString}";
        }

        
        public void Log(string message, LogSeverity severity = LogSeverity.Info, object additional = null)
        {
            var logObject = new BotLogObject(message, severity, additional);
            LogAndStore(message, severity, logObject);
            LogToDiscord(message, severity);
        }

        private void LogAndStore<T>(string message, LogSeverity severity, T logObject)
        {
            Console.WriteLine(MakeLogMessage(message, severity));
            if (Config.LogToDatabase)
            {
                Store.Store(logObject);
            }
        }

        private void LogToDiscord(string message, LogSeverity severity, LogContext context = null)
        {
            if (!Config.LogToChannel)
            {
                return;
            }

            if (message.Contains("is blocking the gateway task.", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }
            try
            {
                var channel = Client?.GetGuild(Config.GuildId)?.GetTextChannel(Config.ChannelId);
                var embed = new EmbedBuilder
                {
                    Description = message.FixLength(),
                    Color = LogSeverityAsColor(severity),
                    Title = severity.ToString()
                };

                if (context != null)
                {
                    embed = embed.AddField("Context", 
                    $"Channel: {context.channelId}\n" +
                    $"Guild: {context.guildId}\n" +
                    $"User: {context.userId}\n" +
                    $"Message: {context.message}".FixLength());
                }

                channel?.SendMessageAsync(string.Empty, false, embed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Log(string message, LogContext context, LogSeverity severity = LogSeverity.Info, object additional = null)
        {
            var logObject = new CommandLogObject(message, context, severity, additional);
            LogAndStore(message, severity, logObject);
            LogToDiscord(message, severity, context);
        }
    }
}
