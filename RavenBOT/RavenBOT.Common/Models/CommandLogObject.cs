using System;
using Discord;

namespace RavenBOT.Models
{
    public class CommandLogObject
    {
        public string Message { get; set; }
        public LogContext Context { get; set; }
        public LogSeverity Level { get; }
        public DateTime TimeStamp { get; set; }
        public object Additional { get; set; }

        public CommandLogObject(string message, LogContext context, LogSeverity level, object additional = null)
        {
            Message = message;
            Context = context;
            TimeStamp = DateTime.UtcNow;
            Additional = additional;
            Level = level;
        }
    }
}