using System;
using System.Collections.Generic;
using System.Text;

namespace RavenBOT.Modules.Lithium.Models
{
    //Used for ticket settings for a server
    public class TicketGuild
    {
        public TicketGuild(ulong guildId)
        {
            GuildId = guildId;
            TicketChannelId = 0;
        }

        public static string DocumentName(ulong guildId)
        {
            return $"TicketGuild-{guildId}";
        }

        public ulong GuildId { get; }

        public ulong TicketChannelId { get; set; }

        //TODO: Required permissions to create tickets?
    }
}
