using Discord.Commands;

namespace RavenBOT.Common
{
    public class LogContext
    {
        public ulong userId { get; set; }
        public string userName { get; set; }
        public ulong guildId { get; set; }
        public string guildName { get; set; }
        public ulong channelId { get; set; }
        public string channelName { get; set; }
        public string message { get; set; }

        public LogContext(ICommandContext context)
        {
            userId = context.User?.Id ?? 0;
            userName = context.User?.Username;
            guildId = context.Guild?.Id ?? 0;
            guildName = context.Guild?.Name;
            channelId = context.Channel?.Id ?? 0;
            channelName = context.Channel?.Name;
            message = context.Message?.Content;
        }
    }
}