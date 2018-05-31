using Discord;
using RavenBOT.Discord.Context;
using Serilog;
using Serilog.Core;

namespace RavenBOT.Handlers
{
    public static class LogHandler
    {
        private static readonly Logger log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        public static string Left(this string s, int len)
        {
            return s.Length == len ? s : (s.Length < len ? s.PadRight(len) : s.Substring(0, len));
        }

        public static void LogMessage(Context Context, string message = null, LogSeverity Level = LogSeverity.Info)
        {
            var custom = $"G: {Context.Guild.Name.Left(20)} || C: {Context.Channel.Name.Left(20)} || U: {Context.User.Username.Left(20)} || M: {Context.Message.Content.Left(100)}";

            if (message != null)
            {
                custom += $"\nE: {message.Left(100)}";
            }

            switch (Level)
            {
                case LogSeverity.Info:
                    log.Information(custom);
                    break;
                case LogSeverity.Warning:
                    log.Warning(custom);
                    break;
                case LogSeverity.Error:
                    log.Error(custom);
                    break;
                case LogSeverity.Debug:
                    log.Debug(custom);
                    break;
                case LogSeverity.Critical:
                    log.Fatal(custom);
                    break;
                case LogSeverity.Verbose:
                    log.Verbose(custom);
                    break;
                default:
                    log.Information(message);
                    break;
            }
        }


        public static void LogMessage(string message, LogSeverity Level = LogSeverity.Info)
        {
            switch (Level)
            {
                case LogSeverity.Info:
                    log.Information(message);
                    break;
                case LogSeverity.Warning:
                    log.Warning(message);
                    break;
                case LogSeverity.Error:
                    log.Error(message);
                    break;
                case LogSeverity.Debug:
                    log.Debug(message);
                    break;
                case LogSeverity.Critical:
                    log.Fatal(message);
                    break;
                case LogSeverity.Verbose:
                    log.Verbose(message);
                    break;
                default:
                    log.Information(message);
                    break;
            }
        }
    }
}