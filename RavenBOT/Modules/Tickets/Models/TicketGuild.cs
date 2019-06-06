using System;
using System.Collections.Generic;
using System.Text;

namespace RavenBOT.Modules.Tickets.Models
{
    //Used for ticket settings for a server
    public class TicketGuild
    {
        public TicketGuild(ulong guildId)
        {
            GuildId = guildId;
            TicketChannelId = 0;
        }

        public TicketGuild() {}

        public static string DocumentName(ulong guildId)
        {
            return $"TicketGuild-{guildId}";
        }

        public ulong GuildId { get; set; }

        //Role ids that are allowed to manage tickets like admins
        public List<ulong> TicketManagers {get;set;} = new List<ulong>();

        //List of role ids that are allowed to create tickets (by default all roles are allowed)
        public List<ulong> TicketCreatorWhitelist {get;set;} = new List<ulong>();

        public ulong TicketChannelId { get; set; }

        public bool UseVoting {get;set;} = true;

        //Indicates whether to nofity the creator of a ticket when it's state is changed.
        public bool NotifyCreatorOnStateChange {get;set;} = true;
    }
}
