namespace RavenBOT.Handlers
{
    using System;

    using global::Discord;

    using RavenBOT.Discord.Context;
    using RavenBOT.Models;

    using Serilog;
    using Serilog.Core;

    /// <summary>
    /// The Log handler.
    /// </summary>
    public static class LogHandler
    {
        /// <summary>
        /// The Log.
        /// </summary>
        private static readonly Logger Log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        /// <summary>
        /// Ensures a string is aligned and kept to the specified length
        /// Uses substring if it is too long and pads if too short.
        /// </summary>
        /// <param name="s">
        /// The string to modify
        /// </param>
        /// <param name="len">
        /// The desired string length
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string Left(this string s, int len)
        {
            return s.Length == len ? s : (s.Length < len ? s.PadRight(len) : s.Substring(0, len));
        }

        /// <summary>
        /// Logs a message to console with the specified severity. Includes info based on context
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="error">
        /// Optional error message.
        /// </param>
        /// <param name="logSeverity">
        /// The Severity of the message
        /// </param>
        public static void LogMessage(Context context, string error = null, LogSeverity logSeverity = LogSeverity.Info)
        {
            var custom = $"G: {context.Guild.Name.Left(20)} || C: {context.Channel.Name.Left(20)} || U: {context.User.Username.Left(20)} || M: {context.Message.Content.Left(100)}";

            if (error != null)
            {
                custom += $"\nE: {error}";
            }

            switch (logSeverity)
            {
                case LogSeverity.Info:
                    Log.Information(custom);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(custom);
                    break;
                case LogSeverity.Error:
                    Log.Error(custom);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(custom);
                    break;
                case LogSeverity.Critical:
                    Log.Fatal(custom);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(custom);
                    break;
                default:
                    Log.Information(custom);
                    break;
            }
        }

        /// <summary>
        /// Logs a message to console
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="logSeverity">
        /// The severity of the message
        /// </param>
        public static void LogMessage(string message, LogSeverity logSeverity = LogSeverity.Info)
        {
            switch (logSeverity)
            {
                case LogSeverity.Info:
                    Log.Information(message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message);
                    break;
                case LogSeverity.Error:
                    Log.Error(message);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message);
                    break;
                case LogSeverity.Critical:
                    Log.Fatal(message);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message);
                    break;
                default:
                    Log.Information(message);
                    break;
            }
        }

        /// <summary>
        /// Prints application info to console
        /// </summary>
        /// <param name="settings">
        /// The settings.
        /// </param>
        /// <param name="config">
        /// The config.
        /// </param>
        public static void PrintApplicationInformation(DatabaseObject settings, ConfigModel config)
        {
            Console.WriteLine("-> INFORMATION\n" +
                              $"-> Database URL: {settings?.URL}\n" +
                              $"-> Database Name: {settings?.Name}\n" +
                              $"-> Prefix: {config.Prefix}\n" +
                              "    Author: PassiveModding | Discord: https://discord.me/Passive\n" +
                              $"=======================[ {DateTime.UtcNow} ]=======================");
        }
    }
}