using System;
using System.Linq;
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
    //TODO: Custom precondition that allows for commands to work based off 'moderator' list in modconfig
    public partial class Moderation : InteractiveBase<ShardedCommandContext>
    {      
        public ModerationHandler ModHandler {get;}

        public Moderation(IDatabase database, DiscordShardedClient client)
        {
            ModHandler = new ModerationHandler(database, client);
        }   

        [Command("HackBan")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task MaxWarnings(ulong userId, [Remainder]string reason = null)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);

            var caseId = config.AddLogAction(userId, Context.User.Id, ActionConfig.Log.LogAction.Ban, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            await Context.Guild.AddBanAsync(userId, 0, reason);

            await ReplyAsync($"#{caseId} Hackbanned user with ID {userId}");
        }

        [Command("SetMaxWarnings")]
        public async Task MaxWarnings(int max)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MaxWarnings = max;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Max Warnings is now: {max}");
        }

        [Command("MaxWarningsAction")]
        public async Task MaxWarningsAction(ActionConfig.Action action)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MaxWarningsAction = action;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Max Warnings Action is now: {action}");
        }

        [Command("SetDefaultSoftBanTime")]
        public async Task DefaultSoftBanTime(TimeSpan time)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.SoftBanLength = time;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"By Default users will be softbanned for {time.GetReadableLength()}");
        }

        [Command("SetDefaultMuteTime")]
        public async Task DefaultMuteTime(TimeSpan time)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MuteLength = time;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"By Default users will be muted for {time.GetReadableLength()}");
        }

        [Command("SetReason")]
        public async Task SetReason(int caseId, [Remainder]string reason)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);

            var action = config.LogActions.FirstOrDefault(x => x.CaseId == caseId);
            if (action == null)
            {
                await ReplyAsync("Invalid Case ID");
                return;
            }

            //Check if reason is updated or list needs to be re-updated
            if (action.Reason == null)
            {
                action.Reason = reason;
                await ReplyAsync("Reason set.");
            }
            else
            {
                action.Reason =  $"**Original Reason**\n{action.Reason}**Updated Reason**\n{reason}";
                //TODO: Show action in embed
                await ReplyAsync("Appended reason to message.");
            }

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
        }

        [Command("ban")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task BanUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //Setting for prune days is needed
            //Log this to some config file?
            await user.Guild.AddBanAsync(user, 0, reason);
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Ban, reason);
            await ReplyAsync($"#{caseId} {user.Mention} was banned by {Context.User.Mention} for {reason ?? "N/A"}");

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
        }

        [Command("kick")]
        [RequireBotPermission(Discord.GuildPermission.KickMembers)]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task KickUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            //Log this to some config file?
            await user.KickAsync(reason);
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Kick, reason);
            await ReplyAsync($"#{caseId} {user.Mention} was kicked by {Context.User.Mention} for {reason ?? "N/A"}");

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
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
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Warn, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            ModHandler.Save(userConfig, ActionConfig.ActionUser.DocumentName(user.Id, Context.Guild.Id));
            //TODO: If reason is not set, generate ticket-like ID and have command where they can set the reason afterwards
            await ReplyAsync($"#{caseId} {user.Mention} was warned by {Context.User.Mention} for {reason ?? "N/A"}");
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
           await MuteUser(user, null, reason);
        }

        [Command("mute")]
        //TODO: Mod permissions
        public async Task MuteUser(SocketGuildUser user, TimeSpan? time = null, [Remainder]string reason = null)
        {
            //TODO: accept time for user to be muted or get default from config
            //Log this to some config file?
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var muteRole = await ModHandler.GetOrCreateMuteRole(config, Context.Guild);
            await user.AddRoleAsync(muteRole);

            if (time == null)
            {
                time = config.MuteLength;
            }

            ModHandler.TimedActions.Users.Add(new Models.TimeTracker.User(user.Id, Context.Guild.Id, Models.TimeTracker.User.TimedAction.Mute, time.Value));
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Mute, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            ModHandler.Save(ModHandler.TimedActions, TimeTracker.DocumentName);
            var expiryTime = DateTime.UtcNow + time.Value;
            await ReplyAsync($"#{caseId} {user.Mention} has been muted for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()}\n**Reason:** {reason ?? "N/A"}");
            //TODO: Responses and query mutes + reasons
        }

        [Command("softban")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        //TODO: Mod permissions
        public async Task SoftBanUser(SocketGuildUser user, [Remainder]string reason = null)
        {
            await SoftBanUser(user, null, reason);
        }

        [Command("softban")]
        [RequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RequireUserPermission(Discord.GuildPermission.BanMembers)]
        //TODO: Mod permissions
        public async Task SoftBanUser(SocketGuildUser user, TimeSpan? time = null, [Remainder]string reason = null)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            await Context.Guild.AddBanAsync(user, 0, reason);

            if (time == null)
            {
                time = config.MuteLength;
            }

            ModHandler.TimedActions.Users.Add(new Models.TimeTracker.User(user.Id, Context.Guild.Id, Models.TimeTracker.User.TimedAction.SoftBan, time.Value));
            ModHandler.Save(ModHandler.TimedActions, TimeTracker.DocumentName);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Mute, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            var expiryTime = DateTime.UtcNow + time.Value;
            await ReplyAsync($"#{caseId} {user.Mention} has been SoftBanned for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()} \n**Reason:** {reason ?? "N/A"}");

            //TODO: Message user?
        }
    }
}