using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Modules.Tickets.Methods;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Tickets.Modules
{
    [Group("ticket ")]
    [RequireContext(ContextType.Guild)]
    public class Ticket : InteractiveBase<ShardedCommandContext>
    {
        public DiscordShardedClient Client { get; }
        private TicketService TicketService { get; }

        public Ticket(IDatabase database, DiscordShardedClient client)
        {
            Client = client;
            TicketService = new TicketService(database);
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionRemoved(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //Check if live message, update up/downvote count
            if (reaction.Emote.Name == "👍" || reaction.Emote.Name == "👎")
            {
                var ticket = TicketService.GetTicket(reaction.MessageId);
                if (ticket != null)
                {
                    if (reaction.Emote.Name == "👍")
                    {
                        ticket.RemoveUpvote(reaction.UserId);
                        await TicketService.UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, ticket);
                        TicketService.SaveTicket(ticket);
                    }

                    if (reaction.Emote.Name == "👎")
                    {
                        ticket.RemoveDownvote(reaction.UserId);
                        await TicketService.UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, ticket);
                        TicketService.SaveTicket(ticket);
                    }
                }
            }
        }

        private async Task ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            //Check if live message, update up/downvote count
            if (reaction.Emote.Name == "👍" || reaction.Emote.Name == "👎")
            {
                var ticket = TicketService.GetTicket(reaction.MessageId);
                if (ticket != null)
                {
                    if (reaction.Emote.Name == "👍")
                    {
                        ticket.Upvote(reaction.UserId);
                        await TicketService.UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, ticket);
                        TicketService.SaveTicket(ticket);
                    }

                    if (reaction.Emote.Name == "👎")
                    {
                        ticket.Downvote(reaction.UserId);
                        await TicketService.UpdateLiveMessageAsync((channel as SocketTextChannel).Guild, ticket);
                        TicketService.SaveTicket(ticket);
                    }
                }
            }
        }

        [Command("SetChannel")]
        public async Task SetChannel()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            guild.TicketChannelId = Context.Channel.Id;
            TicketService.SaveGuild(guild);

            var _ = Task.Run(async () => { 
                foreach (var ticket in TicketService.GetTickets(Context.Guild.Id).OrderBy(x => x.TicketId))
                {
                    await Context.Channel.SendMessageAsync("", false, ticket.GenerateEmbed(Context.Guild).Build());
                }

            });
            

            await ReplyAsync("Channel has been set.");
        }

        [Command("Open")]
        public async Task OpenTicket([Remainder]string message)
        {
            try
            {
                var ticket = await TicketService.NewTicket(Context, message);
                ticket.Item2?.AddReactionsAsync(new IEmote[]{new Emoji("👍"), new Emoji("👎")});
                await ReplyAsync($"Ticket #{TicketService.TicketCount(Context.Guild)} has been created. {(ticket.Item2 == null ? "" : $"\nhttps://discordapp.com/channels/{Context.Guild.Id}/{ticket.Item2.Channel.Id}/{ticket.Item2.Id}")}");
            
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("Re-Open")]
        public async Task ReOpenTicket(int ticketId, [Remainder]string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            if (ticket.AuthorId == Context.User.Id)
            {
                ticket.SetState(Models.Ticket.TicketState.open, reason ?? "Re-opened by creator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Re-opened.");
            }
            else if (Context.User is SocketGuildUser g && g.GuildPermissions.Administrator)
            {
                ticket.SetState(Models.Ticket.TicketState.open, reason ?? "Re-opened by administrator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Re-opened. (as admin)");
            }
        }

        
        [Command("Delete")]
        public async Task DeleteTicket(int ticketId)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            if (ticket.AuthorId == Context.User.Id)
            {
                TicketService.RemoveTicket(Context.Guild.Id, ticketId);
                await ReplyAsync("Deleted.");
            }
            else if (Context.User is SocketGuildUser g && g.GuildPermissions.Administrator)
            {
                TicketService.RemoveTicket(Context.Guild.Id, ticketId);
                await ReplyAsync("Deleted. (as admin)");
            }
        }

        [Command("Close")]
        public async Task CloseTicket(int ticketId, [Remainder]string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            if (ticket.AuthorId == Context.User.Id)
            {
                ticket.SetState(Models.Ticket.TicketState.close, reason ?? "Closed by creator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Closed.");
            }
            else if (Context.User is SocketGuildUser g && g.GuildPermissions.Administrator)
            {
                ticket.SetState(Models.Ticket.TicketState.close, reason ?? "Closed by administrator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Closed. (as admin)");
            }
        }

        [Command("Solve")]
        public async Task SolveTicket(int ticketId, [Remainder]string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            if (ticket.AuthorId == Context.User.Id)
            {
                ticket.SetState(Models.Ticket.TicketState.solved, reason ?? "Set solved by creator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Solved.");
            }
            else if (Context.User is SocketGuildUser g && g.GuildPermissions.Administrator)
            {
                ticket.SetState(Models.Ticket.TicketState.solved, reason ?? "Set solved by administrator.");
                TicketService.SaveTicket(ticket);
                await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
                await ReplyAsync("Solved. (as admin)");
            }
            else
            {
                await ReplyAsync("You must be an admin or the creator of the ticket in order to solve it.");
            }
        }

        [Command("Hold")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HoldTicket(int ticketId, [Remainder]string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            ticket.SetState(Models.Ticket.TicketState.on_hold, reason ?? "Set on hold by administrator.");
            TicketService.SaveTicket(ticket);
            await TicketService.UpdateLiveMessageAsync(Context.Guild, ticket);
            await ReplyAsync("Set on hold.");
        }

        [Command("View")]
        public async Task ViewTicket(int ticketId)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            await ReplyAsync("", false, ticket.GenerateEmbed(Context.Guild).Build());
        }
    }
}
