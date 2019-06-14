using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;

namespace RavenBOT.Modules.Tickets.Models
{
    public class Ticket
    {
        public Ticket(ulong guildId, ulong authorId, int ticketNumber, string message)
        {
            GuildId = guildId;
            AuthorId = authorId;
            TicketNumber = ticketNumber;
            LiveMessageId = 0;

            Message = message;

            Upvoters = new List<ulong>();
            Downvoters = new List<ulong>();

            SetState(TicketState.open);
        }

        public Ticket() { }

        public static string DocumentName(ulong guildId, int ticketId)
        {
            return $"Ticket-{guildId}-{ticketId}";
        }

        public int TicketNumber { get; set; }

        public ulong GuildId { get; set; }

        public ulong LiveMessageId { get; set; }

        public ulong AuthorId { get; set; }

        private List<ulong> Upvoters { get; set; } = new List<ulong>();
        private List<ulong> Downvoters { get; set; } = new List<ulong>();

        public EmbedBuilder GenerateEmbed(SocketGuild guild, bool useVoting)
        {
            var builder = new EmbedBuilder
            {
                Title = $"Ticket #{TicketNumber}",
                Color = GetTicketColor(),
                Description = Message?.FixLength(512),
                Author = GetAuthorBuilder(guild)
            };

            if (useVoting)
            {
                builder.Footer = GetFooterBuilder();
            }

            if (StateMessage != null)
            {
                builder.AddField($"{State} Message", StateMessage.FixLength(512));
            }

            return builder;
        }

        private EmbedFooterBuilder GetFooterBuilder()
        {
            var builder = new EmbedFooterBuilder
            {
                Text = $"👍{Upvoters.Count} 👎{Downvoters.Count}"
            };

            return builder;
        }

        private EmbedAuthorBuilder GetAuthorBuilder(SocketGuild guild)
        {
            var user = guild.GetUser(AuthorId);
            if (user != null)
            {
                return new EmbedAuthorBuilder
                {
                IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
                Name = $"{user.Username}#{user.Discriminator}"
                };
            }

            return null;
        }

        private Color GetTicketColor()
        {
            if (State == TicketState.open)
            {
                return Color.Blue;
            }

            if (State == TicketState.solved)
            {
                return Color.Green;
            }
            if (State == TicketState.on_hold)
            {
                return Color.Gold;
            }

            if (State == TicketState.close)
            {
                return Color.Red;
            }

            return Color.Red;
        }

        public void RemoveUpvote(ulong userId)
        {
            if (Upvoters.Contains(userId))
            {
                Upvoters.Remove(userId);
            }
        }

        public void RemoveDownvote(ulong userId)
        {
            if (Downvoters.Contains(userId))
            {
                Downvoters.Remove(userId);
            }
        }

        public void Downvote(ulong userId)
        {
            if (Downvoters.Contains(userId))
            {
                Downvoters.Remove(userId);
            }
            else
            {
                Downvoters.Add(userId);
            }

            if (Upvoters.Contains(userId))
            {
                Upvoters.Remove(userId);
            }
        }

        public void Upvote(ulong userId)
        {
            if (Upvoters.Contains(userId))
            {
                Upvoters.Remove(userId);
            }
            else
            {
                Upvoters.Add(userId);
            }

            if (Downvoters.Contains(userId))
            {
                Downvoters.Remove(userId);
            }
        }

        public string Message { get; set; }

        public enum TicketState
        {
            [Description("Open")]
            open, [Description("Solve")]
            solved, [Description("Hold")]
            on_hold, [Description("Close")]
            close
        }

        private TicketState State { get; set; }
        private string StateMessage { get; set; }

        public TicketState GetState()
        {
            return State;
        }

        public string GetStateMessage()
        {
            return StateMessage;
        }

        public void SetState(TicketState state, string message = null)
        {
            State = state;
            StateMessage = message;
        }

        //TODO: Comments?
    }
}