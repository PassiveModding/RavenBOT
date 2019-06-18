namespace RavenBOT.Modules.Automessage.Models
{
    public class AutomessageChannel
    {
        public AutomessageChannel(ulong channelId)
        {
            ChannelId = channelId;
        }

        public AutomessageChannel() {}

        public static string DocumentName(ulong channelId)
        {
            return $"AutomessageChannel-{channelId}";
        }

        public ulong ChannelId { get; set; }
        public string Response { get; set; }
        public int MessageCount { get; set; } = 0;

        //Dictates how many messages must be sent before the bot respo
        public int RespondOn { get; set; } = 25;
    }
}