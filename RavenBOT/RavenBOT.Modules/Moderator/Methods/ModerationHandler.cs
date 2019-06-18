using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RavenBOT.Common;
using RavenBOT.Common.Interfaces;
using RavenBOT.Modules.Moderator.Models;

namespace RavenBOT.Modules.Moderator.Methods
{
    public class ModerationHandler : IServiceable
    {
        public IDatabase Database { get; }

        public TimeTracker TimedActions { get; set; }

        public DiscordShardedClient Client { get; }

        public Timer Timer { get; }
        public ModerationHandler(IDatabase database, DiscordShardedClient client)
        {
            Database = database;
            Client = client;
            TimedActions = Database.Load<TimeTracker>(TimeTracker.DocumentName);

            if (TimedActions == null)
            {
                TimedActions = new TimeTracker();
                Database.Store(TimedActions, TimeTracker.DocumentName);
            }

            Timer = new Timer(TimerEvent, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Tries to get the mute role, if unsuccessful creats a new one.
        /// Will automatically apply relevant permissions for all channels in the guild
        /// </summary>
        /// <param name="config"></param>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<IRole> GetOrCreateMuteRole(ActionConfig config, SocketGuild guild)
        {
            IRole role;
            if (config.MuteRole == 0)
            {
                //If the role isn't set just create it
                role = await guild.CreateRoleAsync("Muted");
                config.MuteRole = role.Id;
                Save(config, ActionConfig.DocumentName(guild.Id));
            }
            else
            {
                role = guild.GetRole(config.MuteRole);
                if (role == null)
                {
                    //In the case that the role was removed or is otherwise unavailable generate a new one
                    role = await guild.CreateRoleAsync("Muted");
                    config.MuteRole = role.Id;
                    Save(config, ActionConfig.DocumentName(guild.Id));
                }

                //Set the default role permissions to deny the only ways the user can communicate in the server
                if (role.Permissions.SendMessages || role.Permissions.AddReactions || role.Permissions.Connect || role.Permissions.Speak)
                {
                    await role.ModifyAsync(x =>
                    {
                        x.Permissions = new GuildPermissions(sendMessages: false, addReactions: false, connect: false, speak: false);
                    });
                }
            }

            foreach (var channel in guild.Channels)
            {
                //Update channel permission overwrites to stop the user from being able to communicate
                if (channel.PermissionOverwrites.All(x => x.TargetId != role.Id))
                {
                    var _ = Task.Run(async() => await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny, connect: PermValue.Deny, speak: PermValue.Deny)));
                }
            }

            return role;
        }

        public void TimerEvent(object _)
        {
            var run = Task.Run(async() =>
            {
                var now = DateTime.UtcNow;
                var modified = false;
                foreach (var user in TimedActions.Users.ToList())
                {
                    if (user.TimeStamp + user.Length < now)
                    {
                        var discordUser = Client.GetGuild(user.GuildId)?.GetUser(user.UserId);
                        if (discordUser == null)
                        {
                            continue;
                        }

                        if (user.Action == TimeTracker.User.TimedAction.Mute)
                        {
                            //Remove mute role from user.
                            var config = GetActionConfig(user.GuildId);
                            var role = discordUser.Guild.GetRole(config.MuteRole);
                            if (role == null)
                            {
                                continue;
                            }

                            await discordUser.RemoveRoleAsync(role);
                        }
                        else if (user.Action == TimeTracker.User.TimedAction.SoftBan)
                        {
                            //Get server, unban user.
                            await discordUser.Guild.RemoveBanAsync(user.UserId);
                        }

                        //Test if removing works
                        TimedActions.Users.Remove(user);
                        modified = true;
                    }
                }

                if (modified)
                {
                    Save(TimedActions, TimeTracker.DocumentName);
                }
            });
        }

        public void SaveModeratorConfig(ModeratorConfig config)
        {
            Database.Store(config, ModeratorConfig.DocumentName(config.GuildId));
        }

        public ModeratorConfig GetOrCreateModeratorConfig(ulong guildId)
        {
            var config = GetModeratorConfig(guildId);
            if (config == null)
            {
                config = new ModeratorConfig
                {
                GuildId = guildId
                };
                SaveModeratorConfig(config);
            }

            return config;
        }

        public ModeratorConfig GetModeratorConfig(ulong guildId)
        {
            return Database.Load<ModeratorConfig>(ModeratorConfig.DocumentName(guildId));
        }

        public ActionConfig GetActionConfig(ulong guildId)
        {
            var config = Database.Load<ActionConfig>(ActionConfig.DocumentName(guildId));
            if (config == null)
            {
                config = new ActionConfig(guildId);
                Database.Store(config, ActionConfig.DocumentName(guildId));
            }

            return config;
        }

        public ActionConfig.ActionUser GetActionUser(ulong guildId, ulong userId)
        {
            var config = Database.Load<ActionConfig.ActionUser>(ActionConfig.ActionUser.DocumentName(userId, guildId));
            if (config == null)
            {
                config = new ActionConfig.ActionUser(userId, guildId);
                Database.Store(config, ActionConfig.ActionUser.DocumentName(userId, guildId));
            }

            return config;
        }

        public void Save<T>(T document, string name = null)
        {
            Database.Store(document, name);
        }
    }
}