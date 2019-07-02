namespace RavenBOT.Common
{
    public class LoggerConfig
    {
        public bool LogToDatabase { get; set; } = true;
        public bool LogToChannel { get; set; } = false;
        public ulong ChannelId { get; set; } = 0;
        public ulong GuildId { get; set; } = 0;
    }
}