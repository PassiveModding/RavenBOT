using System;
using System.Collections.Generic;
using System.Text;

namespace RavenBOT.Modules.Events.Models.Events
{
    public class EventConfig
    {
        public static string DocumentName(ulong id)
        {
            return $"EventConfig-{id}";
        }

        public ulong GuildId { get; }

        public EventConfig(ulong guildId)
        {
            GuildId = guildId;
        }

        public EventConfig() { }

        public ulong ChannelId { get; set; }

        public bool ChannelCreated { get; set; }

        public bool ChannelDeleted { get; set; }

        public bool ChannelUpdated { get; set; }

        public bool UserLeft { get; set; }

        public bool UserJoined { get; set; }

        public bool UserUpdated { get; set; }

        public bool MessageDeleted { get; set; }

        public bool MessageUpdated { get; set; }

        public bool Enabled { get; set; }
    }
}
