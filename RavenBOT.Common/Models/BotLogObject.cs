using Discord;
using System;

namespace RavenBOT.Common
{
    public class BotLogObject
    {
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; }
        public LogSeverity Level { get; }
        public object Additional { get; set; }

        public BotLogObject(string message, LogSeverity level, object additional = null)
        {
            Message = message;
            Additional = additional;
            TimeStamp = DateTime.UtcNow;
            Level = level;
        }
    }
}