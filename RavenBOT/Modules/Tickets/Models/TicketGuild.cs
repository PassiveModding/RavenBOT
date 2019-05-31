﻿using System;
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

        public ulong TicketChannelId { get; set; }

        //TODO: Required permissions to create tickets?
    }
}
