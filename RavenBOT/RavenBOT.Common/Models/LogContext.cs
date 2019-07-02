using Discord.Commands;

namespace RavenBOT.Common
{
    public class LogContext
    {
        public ulong userId { get; set; }
        public ulong guildId { get; set; }
        public ulong channelId { get; set; }
        public string message { get; set; }

        public LogContext(ICommandContext context)
        {
            userId = context.User?.Id ?? 0;
            guildId = context.Channel?.Id ?? 0;
            channelId = context.Channel?.Id ?? 0;
            message = context.Message?.Content;
        }
    }
}