using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Tickets.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Tickets.Methods
{
    public class TicketService : IServiceable
    {
        private IDatabase Database { get; }
        public DiscordShardedClient Client { get; }

        public TicketService(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        private async Task TicketVote(SocketReaction reaction, ISocketMessageChannel channel, bool added)
        {
            //Check if live message, update up/downvote count
            if (reaction.Emote.Name == "👍" || reaction.Emote.Name == "👎")
            {
                if (reaction.UserId == Client.CurrentUser.Id)
                {
                    //We shouldn't log upvotes from the bot itself as it adds the reactions
                    return;
                }
                var ticket = GetTicket(reaction.MessageId);
                if (ticket != null)
                {
                    var tGuild = GetTicketGuild(ticket.GuildId);
                    if (!tGuild.UseVoting)
                    {
                        return;
                    }
                    else if (ticket.GetState() == Ticket.TicketState.solved || ticket.GetState() == Ticket.TicketState.close)
                    {
                        //Ignore votes on closed or solved tickets.
                        return;
                    }

                    if (reaction.Emote.Name == "👍")
                    {
                        if (added)
                        {
                            ticket.Upvote(reaction.UserId);
                        }
                        else
                        {
                            ticket.RemoveUpvote(reaction.UserId);
                        }
                        await UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, tGuild, ticket);
                        //TicketService.SaveTicket(ticket);
                    }

                    if (reaction.Emote.Name == "👎")
                    {
                        if (added)
                        {
                            ticket.Downvote(reaction.UserId);
                        }
                        else
                        {
                            ticket.RemoveDownvote(reaction.UserId);
                        }
                        await UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, tGuild, ticket);
                        //TicketService.SaveTicket(ticket);
                    }
                }
            }
        }

        private async Task ReactionRemoved(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await TicketVote(reaction, channel, false);
        }

        public bool CanCreate(TicketGuild guild, IGuildUser user)
        {
            //Allow all roles if the creator whitelist is empty
            if (!guild.TicketCreatorWhitelist.Any())
            {
                return true;
            }

            //Allow admins and the server owner always
            if (user.GuildPermissions.Administrator || user.Guild.OwnerId == user.Id)
            {
                return true;
            }

            //Allow anyone who is given a ticket manager role
            if (user.RoleIds.Any(x => guild.TicketManagers.Contains(x)))
            {
                return true;
            }

            //Allow anyone who is given a ticket creator role
            if (user.RoleIds.Any(x => guild.TicketCreatorWhitelist.Contains(x)))
            {
                return true;
            }

            return false;
        }

        public bool IsManager(TicketGuild guild, IGuildUser user, Ticket ticket = null)
        {
            //Allow admins and the server owner always
            if (user.GuildPermissions.Administrator || user.Guild.OwnerId == user.Id)
            {
                return true;
            }

            //Allow anyone who is given a ticket manager role
            if (user.RoleIds.Any(x => guild.TicketManagers.Contains(x)))
            {
                return true;
            }

            //Allow the creator of the ticket to manage it
            if (ticket != null && ticket.AuthorId == user.Id)
            {
                return true;
            }

            return false;
        }

        private async Task ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await TicketVote(reaction, channel, true);
        }

        public TicketGuild GetTicketGuild(ulong guildId)
        {
            var guild = Database.Load<TicketGuild>(TicketGuild.DocumentName(guildId));
            if (guild == null)
            {
                guild = new TicketGuild(guildId);
                SaveGuild(guild);
            }

            return guild;
        }

        public void SaveTicket(Ticket ticket)
        {
            Database.Store(ticket, Ticket.DocumentName(ticket.GuildId, ticket.TicketNumber));
        }

        public void SaveGuild(TicketGuild guild)
        {
            Database.Store(guild, TicketGuild.DocumentName(guild.GuildId));
        }

        public void RemoveTicket(ulong guildId, int ticketId)
        {
            Database.Remove<Ticket>(Ticket.DocumentName(guildId, ticketId));
        }

        //Sets or updates the live message
        //TODO: Schedule message updates to reduce ratelimit issues in larger servers causing the bot to be laggy or display incorrect vote count
        public async Task<IUserMessage> UpdateLiveMessageAsync(SocketGuild guild, TicketGuild tGuild, Ticket ticket)
        {
            if (tGuild.TicketChannelId != 0)
            {
                var channel = guild.GetTextChannel(tGuild.TicketChannelId);
                if (channel != null)
                {
                    if (ticket.LiveMessageId == 0)
                    {
                        var msg = await channel.SendMessageAsync("", false, ticket.GenerateEmbed(guild, tGuild.UseVoting).Build());
                        ticket.LiveMessageId = msg.Id;
                        SaveTicket(ticket);
                        return msg;
                    }

                    var message = await channel.GetMessageAsync(ticket.LiveMessageId);

                    if (message != null)
                    {
                        if (message is IUserMessage msg)
                        {
                            await msg.ModifyAsync(x => x.Embed = ticket.GenerateEmbed(guild, tGuild.UseVoting).Build());
                            SaveTicket(ticket);
                            return msg;
                        }
                    }
                    else
                    {
                        var msg = await channel.SendMessageAsync("", false, ticket.GenerateEmbed(guild, tGuild.UseVoting).Build());
                        ticket.LiveMessageId = msg.Id;
                        SaveTicket(ticket);
                        return msg;
                    }
                }
            }

            return null;
        }

        public int TicketCount(SocketGuild guild)
        {
            var documents = Database.Query<Ticket>().Where(x => x.GuildId == guild.Id).ToList();
            int count = documents.Count;
            if (documents.Any())
            {
                count = documents.Max(x => x.TicketNumber);
            }

            //In the case that a ticket was removed form database, use the highest ticket number instead.
            return count;
        }

        public Ticket GetTicket(ShardedCommandContext context, int ticketId)
        {
            var ticket = Database.Load<Ticket>(Ticket.DocumentName(context.Guild.Id, ticketId));
            return ticket;
        }

        public Ticket GetTicket(ulong messageId)
        {
            var ticket = Database.Query<Ticket>().FirstOrDefault(x => x.LiveMessageId == messageId);
            return ticket;
        }
        public async Task<Tuple<Ticket, IUserMessage>> NewTicket(ShardedCommandContext context, string message)
        {
            var tGuild = GetTicketGuild(context.Guild.Id);
            var ticket = new Ticket(context.Guild.Id, context.User.Id, TicketCount(context.Guild) + 1, message);
            SaveTicket(ticket);
            var msg = await UpdateLiveMessageAsync(context.Guild, tGuild, ticket);
            return new Tuple<Ticket, IUserMessage>(ticket, msg);
        }

        public List<Ticket> GetTickets(ulong guildId)
        {
            return Database.Query<Ticket>().Where(x => x.GuildId == guildId).ToList();
        }
    }
}