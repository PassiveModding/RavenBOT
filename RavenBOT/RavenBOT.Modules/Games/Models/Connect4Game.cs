namespace RavenBOT.Modules.Games.Models
{
    public class Connect4Game
    {
        public Connect4Game(ulong channelId)
        {
            ChannelId = channelId;
        }

        public ulong ChannelId { get; set; }
        public bool GameRunning { get; set; }
    }
}