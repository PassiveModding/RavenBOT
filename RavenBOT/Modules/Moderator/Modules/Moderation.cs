using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RavenBOT.Extensions;
using RavenBOT.Modules.Moderator.Methods;
using RavenBOT.Modules.Moderator.Models;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Moderator.Modules
{
    [Group("moderator.")]
    [RequireUserPermission(Discord.GuildPermission.Administrator)]
    public class Moderation : InteractiveBase<ShardedCommandContext>
    {      
        //Moderator role?
        //Unban, hackban, delete warning(s), remove softban

        public ModerationHandler ModHandler {get;}

        public Moderation(IDatabase database, DiscordShardedClient client)
        {
            ModHandler = new ModerationHandler(database, client);
        }   

        [Command("ban")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task BanUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //Setting for prune days is needed
            //Log this to some config file?
            await user.Guild.AddBanAsync(user, 0, reason);
            await ReplyAsync($"{user.Mention} was banned by {Context.User.Mention} for {reason ?? "N/A"}");
        }

        [Command("kick")]
        [RequireBotPermission(Discord.GuildPermission.KickMembers)]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task KickUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //Log this to some config file?
            await user.KickAsync(reason);
            await ReplyAsync($"{user.Mention} was kicked by {Context.User.Mention} for {reason ?? "N/A"}");
        }

        [Command("warn")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireBotPermission(Discord.GuildPermission.KickMembers)]
        //TODO: check user permission level
        public async Task WarnUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //Log this to some config file?
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var userConfig = ModHandler.GetActionUser(Context.Guild.Id, user.Id);

            var warns = userConfig.WarnUser(Context.User.Id, reason);
            ModHandler.Save(userConfig, ActionConfig.ActionUser.DocumentName(user.Id, Context.Guild.Id));
            //TODO: If reason is not set, generate ticket-like ID and have command where they can set the reason afterwards
            await ReplyAsync($"{user.Mention} was warned by {Context.User.Mention} for {reason ?? "N/A"}");
            if (warns > config.MaxWarnings)
            {
                if (config.MaxWarningsAction == Models.ActionConfig.Action.Kick)
                {
                    await user.KickAsync("Exceeded maimum warnings.");
                    await ReplyAsync($"{user.Mention} was kicked for exceeding the max warning count");
                }
                else if (config.MaxWarningsAction == Models.ActionConfig.Action.Ban)
                {
                    await Context.Guild.AddBanAsync(user, 0, "Exceeded maximum warnings.");
                    await ReplyAsync($"{user.Mention} was banned for exceeding the max warning count");
                }
            }
        }

        [Command("mute")]
        //TODO: Mod permissions
        public async Task MuteUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //TODO: accept time for user to be muted or get default from config
            //Log this to some config file?
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var muteRole = await ModHandler.GetOrCreateMuteRole(config, Context.Guild);
            await user.AddRoleAsync(muteRole);
            var length = TimeSpan.FromMinutes(1);
            ModHandler.TimedActions.Users.Add(new Models.TimeTracker.User(user.Id, Context.Guild.Id, Models.TimeTracker.User.TimedAction.Mute, length));
            ModHandler.Save(ModHandler.TimedActions, TimeTracker.DocumentName);
            var expiryTime = DateTime.UtcNow + length;
            await ReplyAsync($"{user.Mention} has been muted for {length.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()}\n**Reason:** {reason ?? "N/A"}");
            //TODO: Responses and query mutes + reasons
        }

        [Command("softban")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        //TODO: Mod permissions
        public async Task SoftBanUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //TODO: accept time for user to be muted or get default from config
            //Log this to some config file?
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            await Context.Guild.AddBanAsync(user, 0, reason);

            var length = TimeSpan.FromMinutes(1);
            ModHandler.TimedActions.Users.Add(new Models.TimeTracker.User(user.Id, Context.Guild.Id, Models.TimeTracker.User.TimedAction.SoftBan, length));
            ModHandler.Save(ModHandler.TimedActions, TimeTracker.DocumentName);
            var expiryTime = DateTime.UtcNow + length;
            await ReplyAsync($"{user.Mention} has been SoftBanned for {length.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()} \n**Reason:** {reason ?? "N/A"}");

            //TODO: Message user?
        }
    }
}