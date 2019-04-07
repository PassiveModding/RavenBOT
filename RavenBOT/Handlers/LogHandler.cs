using System;
using Discord;
using Discord.WebSocket;
using Raven.Client.Documents;
using RavenBOT.Extensions;
using RavenBOT.Models;

namespace RavenBOT.Handlers
{
    public class LogHandler
    {
        private DiscordShardedClient Client { get; }

        private IDocumentStore Store { get; }

        private LoggerConfig Config { get; }

        public LoggerConfig GetLoggerConfig()
        {
            return Config;
        }

        public void SetLoggerConfig(LoggerConfig newConfig)
        {
            using (var session = Store.OpenSession())
            {
                session.Store(newConfig, "LogConfig");
                session.SaveChanges();
            }
        }

        public LogHandler(DiscordShardedClient client, IDocumentStore store)
        {
            Client = client;
            Store = store;
            using (var session = store.OpenSession())
            {
                Config = session.Load<LoggerConfig>("LogConfig");
                if (Config == null)
                {
                    Config = new LoggerConfig();
                    session.Store(Config, "LogConfig");
                    session.SaveChanges();
                }
            }
        }

        public void Log(string message, LogSeverity severity = LogSeverity.Info, object additional = null)
        {
            var logObject = new BotLogObject(message, severity, additional);
            Console.WriteLine(makeLogMessage(message, severity));
            if (Config.LogToDatabase)
            {
                using (var session = Store.OpenSession())
                {
                    session.Store(logObject);
                    session.SaveChanges();
                }
            }

            if (Config.LogToChannel)
            {
                try
                {
                    var channel = Client?.GetGuild(Config.GuildId)?.GetTextChannel(Config.ChannelId);
                    channel?.SendMessageAsync(string.Empty, false, new EmbedBuilder
                    {
                        Description = message.FixLength(),
                        Color = LogSeverityAsColor(severity),
                        Title = severity.ToString()
                    }.Build());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private Discord.Color LogSeverityAsColor(LogSeverity severity)
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

        private string makeLogMessage(string message, LogSeverity severity, LogContext context = null)
        {
            var newSev = severity.ToString().PadRight(20).Substring(0, 4).ToUpper();
            var contextString = "";
            if (context != null)
            {
                contextString = $"\n[U:{context.userId} G:{context.guildId} C:{context.channelId} M:{context.message}]";
            }
            return $"[{newSev}][{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToShortTimeString()}] {message}{contextString}";
        }

        public void Log(string message, LogContext context, LogSeverity severity = LogSeverity.Info, object additional = null)
        {
            var logObject = new CommandLogObject(message, context, severity, additional);
            Console.WriteLine(makeLogMessage(message, severity));
            if (Config.LogToDatabase)
            {
                using (var session = Store.OpenSession())
                {
                    session.Store(logObject);
                    session.SaveChanges();
                }
            }

            if (Config.LogToChannel)
            {
                try
                {
                    var channel = Client?.GetGuild(Config.GuildId)?.GetTextChannel(Config.ChannelId);
                    channel?.SendMessageAsync(string.Empty, false, new EmbedBuilder
                    {
                        Description = message.FixLength(),
                        Color = LogSeverityAsColor(severity),
                        Title = severity.ToString()
                    }.AddField("Context", 
                        $"Channel: {context.channelId}\n" +
                        $"Guild: {context.guildId}\n" +
                        $"User: {context.userId}\n" +
                        $"Message: {context.message}".FixLength()).Build());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
