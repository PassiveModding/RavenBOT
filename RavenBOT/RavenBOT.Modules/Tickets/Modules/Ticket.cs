using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.Tickets.Methods;
using RavenBOT.Modules.Tickets.Models;

namespace RavenBOT.Modules.Tickets.Modules
{
    [Group("ticket")]
    [RequireContext(ContextType.Guild)]
    public class Ticket : InteractiveBase<ShardedCommandContext>
    {
        private TicketService TicketService { get; }

        public Ticket(TicketService ticketService)
        {
            TicketService = ticketService;
        }

        [Command("SetChannel")]
        [Summary("Sets the current channel for ticket updates")]
        [Remarks("Administrators only.")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task SetChannel()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (guild.TicketChannelId == Context.Channel.Id)
            {
                await ReplyAsync("Channel has been set.");
                return;
            }
            guild.TicketChannelId = Context.Channel.Id;
            TicketService.SaveGuild(guild);

            foreach (var ticket in TicketService.GetTickets(Context.Guild.Id).OrderBy(x => x.TicketNumber).ToList())
            {
                var message = await Context.Channel.SendMessageAsync("", false, ticket.GenerateEmbed(Context.Guild, guild.UseVoting).Build());
                ticket.LiveMessageId = message.Id;
                TicketService.SaveTicket(ticket);
            }

            await ReplyAsync("Channel has been set.");
        }

        [Command("ToggleVoting")]
        [Summary("Toggle the ability for users to vote on tickets")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ToggleVotingAsync()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            guild.UseVoting = !guild.UseVoting;
            TicketService.SaveGuild(guild);
            await ReplyAsync($"Ticket voting: {guild.UseVoting}");
        }

        [Command("Open")]
        [Summary("Opens a new ticket with the given message")]
        public async Task OpenTicket([Remainder] string message)
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!TicketService.CanCreate(guild, Context.User as IGuildUser))
            {
                await ReplyAsync("You don't have permissions to create a ticket.");
                return;
            }

            var ticket = await TicketService.NewTicket(Context, message);
            if (guild.UseVoting)
            {
                ticket.Item2?.AddReactionsAsync(new IEmote[] { new Emoji("👍"), new Emoji("👎") });
            }
            await ReplyAsync($"Ticket #{ticket.Item1.TicketNumber} has been created. {(ticket.Item2 == null ? "" : $"\nhttps://discordapp.com/channels/{Context.Guild.Id}/{ticket.Item2.Channel.Id}/{ticket.Item2.Id}")}");
        }

        [Command("Re-Open")]
        [Alias("reopen", "re open")]
        [Summary("Re-opens a closed ticket")]
        public async Task ReOpenTicket(int ticketId, [Remainder] string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }

            var tGuild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!TicketService.IsManager(tGuild, Context.User as IGuildUser, ticket))
            {
                await ReplyAsync("You don't have enough permissions to modify that ticket.");
                return;
            }

            ticket.SetState(Models.Ticket.TicketState.open, reason ?? $"Re-opened by {Context.User.Mention}");
            TicketService.SaveTicket(ticket);
            await TicketService.UpdateLiveMessageAsync(Context.Guild, tGuild, ticket);
            await TryNotifyTicketCreator(tGuild, ticket);
            await ReplyAsync("Re-opened.");
        }

        [Command("Delete")]
        [Summary("Deletes a ticket from the database")]
        public async Task DeleteTicket(int ticketId)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }

            if (!TicketService.IsManager(TicketService.GetTicketGuild(Context.Guild.Id), Context.User as IGuildUser, ticket))
            {
                await ReplyAsync("You don't have enough permissions to modify that ticket.");
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
            else
            {
                await ReplyAsync("You do not have permissions to delete that ticket. NOTE: Deleting tickets is only applicable to admins and the ticket creator.");
            }
        }

        [Command("Close")]
        [Summary("Closes a ticket with the specified reason")]
        public async Task CloseTicket(int ticketId, [Remainder] string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }

            var tGuild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!TicketService.IsManager(tGuild, Context.User as IGuildUser, ticket))
            {
                await ReplyAsync("You don't have enough permissions to modify that ticket.");
                return;
            }

            ticket.SetState(Models.Ticket.TicketState.close, reason ?? $"Closed by {Context.User.Mention}");
            TicketService.SaveTicket(ticket);
            await TicketService.UpdateLiveMessageAsync(Context.Guild, tGuild, ticket);
            await TryNotifyTicketCreator(tGuild, ticket);
            await ReplyAsync("Closed.");
        }

        [Command("Solve")]
        [Summary("Marks a ticket as solved")]
        public async Task SolveTicket(int ticketId, [Remainder] string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }

            var tGuild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!TicketService.IsManager(tGuild, Context.User as IGuildUser, ticket))
            {
                await ReplyAsync("You don't have enough permissions to modify that ticket.");
                return;
            }

            ticket.SetState(Models.Ticket.TicketState.solved, reason ?? $"Solved by {Context.User.Mention}");
            TicketService.SaveTicket(ticket);
            await TicketService.UpdateLiveMessageAsync(Context.Guild, tGuild, ticket);
            await TryNotifyTicketCreator(tGuild, ticket);
            await ReplyAsync("Solved.");
        }

        [Command("Hold")]
        [Summary("Marks a ticket as on hold")]
        public async Task HoldTicket(int ticketId, [Remainder] string reason = null)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }

            var tGuild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!TicketService.IsManager(tGuild, Context.User as IGuildUser, ticket))
            {
                await ReplyAsync("You don't have enough permissions to modify that ticket.");
                return;
            }

            ticket.SetState(Models.Ticket.TicketState.on_hold, reason ?? "Set on hold by administrator.");
            TicketService.SaveTicket(ticket);
            await TicketService.UpdateLiveMessageAsync(Context.Guild, tGuild, ticket);
            await TryNotifyTicketCreator(tGuild, ticket);
            await ReplyAsync("Set on hold.");
        }

        [Command("View")]
        [Summary("Displays the specified ticket")]
        public async Task ViewTicket(int ticketId)
        {
            var ticket = TicketService.GetTicket(Context, ticketId);
            if (ticket == null)
            {
                await ReplyAsync("No ticket found with that id.");
                return;
            }
            await ReplyAsync("", false, ticket.GenerateEmbed(Context.Guild, TicketService.GetTicketGuild(Context.Guild.Id).UseVoting).Build());
        }

        [Command("ToggleStateNotifications")]
        [Summary("Toggles the notification of users when a ticket's state is changed")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ToggleStateNotifications()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            guild.NotifyCreatorOnStateChange = !guild.NotifyCreatorOnStateChange;
            TicketService.SaveGuild(guild);

            await ReplyAsync($"Notify ticket creator on state change: {guild.NotifyCreatorOnStateChange}");
        }

        [Command("AddCreatorRole")]
        [Summary("Adds a creator role to the ticket config")]
        [Remarks("Creator roles whitelist the use of the open ticket command to specific role(s)")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddCreatorRole(IRole role)
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (guild.TicketCreatorWhitelist.Contains(role.Id))
            {
                await ReplyAsync("This is already a creator role.");
                return;
            }
            guild.TicketCreatorWhitelist.Add(role.Id);
            TicketService.SaveGuild(guild);

            await ReplyAsync("Role has been added.");
        }

        [Command("AddManagerRole")]
        [Summary("Adds a manager role to the ticket config")]
        [Remarks("Manager roles limit the use of ticket clone/solve/hold/re-open commands to specific role(s) + admins")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddManagerRole(IRole role)
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (guild.TicketManagers.Contains(role.Id))
            {
                await ReplyAsync("This is already a manager role.");
                return;
            }
            guild.TicketManagers.Add(role.Id);
            TicketService.SaveGuild(guild);

            await ReplyAsync("Role has been added.");
        }

        [Command("RemoveManagerRole")]
        [Summary("Removes a manager role from the ticket config")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveManagerRole(ulong roleId)
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            guild.TicketManagers.Remove(roleId);
            TicketService.SaveGuild(guild);

            await ReplyAsync("Role has been removed.");
        }

        [Command("RemoveManagerRole")]
        [Summary("Removes a manager role from the ticket config")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveManagerRole(IRole role)
        {
            await RemoveManagerRole(role.Id);
        }

        [Command("RemoveCreatorRole")]
        [Summary("Removes a creator role from the ticket config")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveCreatorRole(ulong roleId)
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            guild.TicketCreatorWhitelist.Remove(roleId);
            TicketService.SaveGuild(guild);

            await ReplyAsync("Role has been removed.");
        }

        [Command("RemoveCreatorRole")]
        [Summary("Removes a creator role from the ticket config")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveCreatorRole(IRole role)
        {
            await RemoveCreatorRole(role.Id);
        }

        [Command("CreatorRoles")]
        [Summary("Shows all ticket creator roles for the server")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreatorRoles()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!guild.TicketCreatorWhitelist.Any())
            {
                await ReplyAsync("There are no ticket creator roles. Ie. Anyone in this server can create a ticket.");
                return;
            }
            var mentionlist = Context.Guild.GetMentionList(guild.TicketCreatorWhitelist);
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = "Ticket Creator Roles",
                    Description = mentionlist.FixLength()
            }.Build());
        }

        [Command("ManagerRoles")]
        [Summary("Shows all ticket manager roles for the server")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ManagerRoles()
        {
            var guild = TicketService.GetTicketGuild(Context.Guild.Id);
            if (!guild.TicketCreatorWhitelist.Any())
            {
                await ReplyAsync("There are no ticket manager roles. Ie. Only admins or the creator of the ticket can modify tickets");
                return;
            }
            var mentionlist = Context.Guild.GetMentionList(guild.TicketManagers);
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Title = "Ticket Manager Roles",
                    Description = mentionlist.FixLength()
            }.Build());
        }

        public async Task TryNotifyTicketCreator(TicketGuild config, Models.Ticket ticket)
        {
            //No need to notify the user if they're the one making changes
            if (Context.User.Id == ticket.AuthorId)
            {
                //return;
            }

            if (config.NotifyCreatorOnStateChange)
            {
                await Context.Guild.DownloadUsersAsync();
                var target = Context.Guild.GetUser(ticket.AuthorId);
                if (target != null)
                {
                    try
                    {
                        await target.SendMessageAsync($"A ticket you created in {Context.Guild.Name} has been updated.\n" +
                            $"https://discordapp.com/channels/{Context.Guild.Id}/{config.TicketChannelId}\n" +
                            $"State changed to: {ticket.GetState()}\n" +
                            $"Reason: {ticket.GetStateMessage()}".FixLength());
                    }
                    catch
                    {
                        //
                    }
                }
            }
        }
    }
}