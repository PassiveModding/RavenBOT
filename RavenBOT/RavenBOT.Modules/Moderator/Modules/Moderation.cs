using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using MoreLinq;
using RavenBOT.Common.Attributes;
using RavenBOT.Common.Services;
using RavenBOT.Extensions;
using RavenBOT.Modules.Moderator.Methods;
using RavenBOT.Modules.Moderator.Models;

namespace RavenBOT.Modules.Moderator.Modules
{
    [Group("mod")]
    [Preconditions.Moderator]
    [RavenRequireContext(ContextType.Guild)]
    public partial class Moderation : InteractiveBase<ShardedCommandContext>
    {
        public ModerationHandler ModHandler { get; }
        public HelpService HelpService { get; }

        public Moderation(ModerationHandler modHandler, HelpService helpService)
        {
            ModHandler = modHandler;
            HelpService = helpService;
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var res = await HelpService.PagedHelpAsync(Context, true, new List<string>
            {
                "mod"
            }, "This module handles relevant moderator commands such as kicking/banning etc.");

            if (res != null)
            {
                await PagedReplyAsync(res, new ReactionList
                {
                    Backward = true,
                        First = false,
                        Forward = true,
                        Info = false,
                        Jump = true,
                        Last = false,
                        Trash = true
                });
            }
            else
            {
                await ReplyAsync("N/A");
            }
        }

        public async Task<bool> IsActionable(SocketGuildUser target, SocketGuildUser currentUser)
        {
            if (target.GuildPermissions.Administrator || target.GuildPermissions.BanMembers)
            {
                await ReplyAsync("You cannot perform actions on admins or users with ban permissions");
                return false;
            }
            else if (target.Hierarchy >= currentUser.Hierarchy)
            {
                await ReplyAsync("You cannot perform actions on users with an equal or higher position than you.");
                return false;
            }

            return true;
        }

        [Command("AddMod")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task AddModeratorAsync(IRole role)
        {
            var modConfig = ModHandler.GetOrCreateModeratorConfig(Context.Guild.Id);
            modConfig.ModeratorRoles = modConfig.ModeratorRoles.Where(x => x != role.Id).ToList();
            modConfig.ModeratorRoles.Add(role.Id);
            ModHandler.SaveModeratorConfig(modConfig);
            await ReplyAsync("Mod role added.");
        }

        [Command("DelMod")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveModeratorAsync(IRole role)
        {
            await RemoveModeratorAsync(role.Id);
        }

        [Command("Moderators")]
        public async Task ViewModeratorsAsync()
        {
            var config = ModHandler.GetModeratorConfig(Context.Guild.Id);
            var roles = Context.Guild.Roles.Where(x => x.Permissions.Administrator).ToList();
            if (config != null)
            {
                roles.AddRange(Context.Guild.Roles.Where(x => config.ModeratorRoles.Contains(x.Id)));
            }
            roles = roles.DistinctBy(x => x.Id).ToList();
            await ReplyAsync("", false, string.Join("\n", roles.OrderByDescending(x => x.Position).Select(x => x.Permissions.Administrator ? $"[Admin] {x.Mention}" : $"[Mod] {x.Mention}")).QuickEmbed());
        }

        [Command("DelMod")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveModeratorAsync(ulong roleId)
        {
            var modConfig = ModHandler.GetOrCreateModeratorConfig(Context.Guild.Id);
            modConfig.ModeratorRoles = modConfig.ModeratorRoles.Where(x => x != roleId).ToList();
            ModHandler.SaveModeratorConfig(modConfig);
            await ReplyAsync("Mod role removed.");
        }

        [Command("HackBan")]
        [RavenRequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RavenRequireUserPermission(Discord.GuildPermission.BanMembers)]
        [Summary("Bans a user based on their user ID")]
        public async Task HackBanAsync(ulong userId, [Remainder] string reason = null)
        {
            var gUser = Context.Guild.GetUser(userId);
            if (gUser != null)
            {
                if (!await IsActionable(gUser, Context.User as SocketGuildUser))
                {
                    return;
                }
            }

            var config = ModHandler.GetActionConfig(Context.Guild.Id);

            var caseId = config.AddLogAction(userId, Context.User.Id, ActionConfig.Log.LogAction.Ban, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            await Context.Guild.AddBanAsync(userId, 0, reason);

            await ReplyAsync($"#{caseId} Hackbanned user with ID {userId}");

            if (gUser != null)
            {
                await ModHandler.LogMessageAsync(Context, $"#{caseId} Hackbanned user with ID {userId}", gUser, reason);
            }
            else
            {
                await ModHandler.LogMessageAsync(Context, $"#{caseId} Hackbanned user with ID {userId}", userId, reason);
            }
        }

        [Command("SetMaxWarnings")]
        [Summary("Sets the most warnings a user can receive before an action is taken")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task MaxWarnings(int max)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MaxWarnings = max;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Max Warnings is now: {max}");
        }

        [Command("MaxWarningsActions")]
        [Summary("Displays the actions that can be taken when a user reaches max warnings")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task MaxWarningsActions()
        {
            var actions = Extensions.StringExtensions.ConvertEnumToDictionary<Models.ActionConfig.Action>();
            await ReplyAsync(string.Join("\n", actions.Keys));
        }

        [Command("MaxWarningsAction")]
        [Summary("Sets the action to take on users who receive too many warnings")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task MaxWarningsAction(ActionConfig.Action action)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MaxWarningsAction = action;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Max Warnings Action is now: {action}");
        }

        [Command("SetDefaultSoftBanTime")]
        [Summary("Sets the default amount of time for a softban")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task DefaultSoftBanTime(TimeSpan time)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.SoftBanLength = time;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"By Default users will be softbanned for {time.GetReadableLength()}");
        }

        [Command("SetDefaultMuteTime")]
        [Summary("Sets the default amount of time for mutes")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task DefaultMuteTime(TimeSpan time)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.MuteLength = time;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"By Default users will be muted for {time.GetReadableLength()}");
        }

        [Command("SetLogChannel")]
        [Summary("Sets the log channel for mod actions")]
        [RavenRequireUserPermission(GuildPermission.Administrator)]
        public async Task SetLogChannelAsync()
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            config.LogChannelId = Context.Channel.Id;
            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ReplyAsync($"Log channel set to the current channel.");
        }

        [Command("SetReason")]
        [Summary("Sets the reason for a specific moderation case")]
        public async Task SetReason(int caseId, [Remainder] string reason)
        {
            var config = ModHandler.GetActionConfig(Context.Guild.Id);

            var action = config.LogActions.FirstOrDefault(x => x.CaseId == caseId);
            if (action == null)
            {
                await ReplyAsync("Invalid Case ID");
                return;
            }

            //Check if reason is updated or list needs to be re-updated
            if (action.Reason == null || action.Reason.Equals("N/A"))
            {
                action.Reason = reason;
                await ReplyAsync("Reason set.");

                await ModHandler.LogMessageAsync(Context, $"#{caseId} reason was set", null, reason);
            }
            else
            {
                action.Reason = $"**Original Reason**\n{action.Reason}**Updated Reason**\n{reason}";
                await ReplyAsync("Appended reason to message.");
                //TODO: Get user if possible.
                await ModHandler.LogMessageAsync(Context, $"#{caseId} reason was updated", null, reason);
            }

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
        }

        [Command("ban")]
        [Summary("Bans a user")]
        [RavenRequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RavenRequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task BanUser(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (!await IsActionable(user, Context.User as SocketGuildUser))
            {
                return;
            }

            //Setting for prune days is needed
            //Log this to some config file?
            await user.Guild.AddBanAsync(user, 0, reason);
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Ban, reason);
            await ReplyAsync($"#{caseId} {user.Mention} was banned by {Context.User.Mention} for {reason ?? "N/A"}");

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ModHandler.LogMessageAsync(Context, $"#{caseId} {user.Mention} was kicked by {Context.User.Mention} for {reason ?? "N/A"}", user, reason);
        }

        [Command("kick")]
        [Summary("Kicks a user from the server")]
        [RavenRequireBotPermission(Discord.GuildPermission.KickMembers)]
        [RavenRequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task KickUser(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (!await IsActionable(user, Context.User as SocketGuildUser))
            {
                return;
            }

            await user.KickAsync(reason);
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Kick, reason);
            await ReplyAsync($"#{caseId} {user.Mention} was kicked by {Context.User.Mention} for {reason ?? "N/A"}");

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));
            await ModHandler.LogMessageAsync(Context, $"#{caseId} {user.Mention} was kicked by {Context.User.Mention}", user, reason);
        }

        [Command("warn")]
        [Summary("Warns a user")]
        public async Task WarnUser(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (!await IsActionable(user, Context.User as SocketGuildUser))
            {
                return;
            }

            //Log this to some config file?
            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            var userConfig = ModHandler.GetActionUser(Context.Guild.Id, user.Id);

            var warns = userConfig.WarnUser(Context.User.Id, reason);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.Warn, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            ModHandler.Save(userConfig, ActionConfig.ActionUser.DocumentName(user.Id, Context.Guild.Id));

            await ReplyAsync($"#{caseId} {user.Mention} was warned by {Context.User.Mention} for {reason ?? "N/A"}");
            await ModHandler.LogMessageAsync(Context, $"#{caseId} {user.Mention} was warned by {Context.User.Mention}", user, reason);

            //TODO: Separate actions for below.
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
                else if (config.MaxWarningsAction == Models.ActionConfig.Action.SoftBan)
                {
                    await SoftBanUser(user, "Exceeded maximum warnings.");
                    await ReplyAsync($"{user.Mention} was soft-banned for `{config.SoftBanLength.GetReadableLength()}` for exceeding the max warning count");
                }
                else if (config.MaxWarningsAction == Models.ActionConfig.Action.Mute)
                {
                    await MuteUser(user, "Exceeded maximum warnings");
                    await ReplyAsync($"{user.Mention} was muted for `{config.SoftBanLength.GetReadableLength()}` for exceeding the max warning count");
                }
            }
        }

        [Command("mute")]
        [Summary("Stops a user from being able to talk")]
        public async Task MuteUser(SocketGuildUser user, [Remainder] string reason = null)
        {
            await MuteUser(user, null, reason);
        }

        [Command("mute")]
        [Summary("Stops a user from being able to talk for the specified amount of time")]
        public async Task MuteUser(SocketGuildUser user, TimeSpan? time = null, [Remainder] string reason = null)
        {
            if (!await IsActionable(user, Context.User as SocketGuildUser))
            {
                return;
            }

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

            await ModHandler.LogMessageAsync(Context, $"#{caseId} {user.Mention} has been muted for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()}", user, reason);
        }

        [Command("softban")]
        [Summary("Bans a user from the server temporarily")]
        [RavenRequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RavenRequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task SoftBanUser(SocketGuildUser user, [Remainder] string reason = null)
        {
            await SoftBanUser(user, null, reason);
        }

        [Command("softban")]
        [Summary("Bans a user from the server for the specified amount of time")]
        [RavenRequireBotPermission(Discord.GuildPermission.BanMembers)]
        [RavenRequireUserPermission(Discord.GuildPermission.BanMembers)]
        public async Task SoftBanUser(SocketGuildUser user, TimeSpan? time = null, [Remainder] string reason = null)
        {
            if (!await IsActionable(user, Context.User as SocketGuildUser))
            {
                return;
            }

            var config = ModHandler.GetActionConfig(Context.Guild.Id);
            
            if (time == null)
            {
                time = config.SoftBanLength;
            }
            var expiryTime = DateTime.UtcNow + time.Value;

            await ReplyAsync($"You have been SoftBanned in {Context.Guild.Name} for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()} \n**Reason:** {reason ?? "N/A"}");

            await Context.Guild.AddBanAsync(user, 0, reason);

            ModHandler.TimedActions.Users.Add(new Models.TimeTracker.User(user.Id, Context.Guild.Id, Models.TimeTracker.User.TimedAction.SoftBan, time.Value));
            ModHandler.Save(ModHandler.TimedActions, TimeTracker.DocumentName);
            var caseId = config.AddLogAction(user.Id, Context.User.Id, ActionConfig.Log.LogAction.SoftBan, reason);

            ModHandler.Save(config, ActionConfig.DocumentName(Context.Guild.Id));

            await ReplyAsync($"#{caseId} {user.Mention} has been SoftBanned for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()} \n**Reason:** {reason ?? "N/A"}");
            await ModHandler.LogMessageAsync(Context, $"#{caseId} {user.Mention} has been SoftBanned for {time.Value.GetReadableLength()}, Expires at: {expiryTime.ToShortDateString()} {expiryTime.ToShortTimeString()}", user, reason);
        }
    }
}