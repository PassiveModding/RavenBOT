using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using RavenBOT.Modules.Levels.Models;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Modules.Levels.Methods
{
    public class LevelService : IServiceable
    {
        public LevelService(IDatabase database, DiscordShardedClient client, LocalManagementService localManagementService)
        {
            Database = database;
            Client = client;
            LocalManagementService = localManagementService;
            Timer = new Timer(TimerEvent, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            Client.MessageReceived += LevelEvent;
        }

        public IDatabase Database { get; }
        public DiscordShardedClient Client { get; }
        public LocalManagementService LocalManagementService { get; }
        public Timer Timer { get; }

        public void TimerEvent(object _)
        {
            UsersUpdated.Clear();
        }

        public Tuple<LevelUser, LevelConfig> GetLevelUser(ulong guildId, ulong userId)
        {
            var config = TryGetLevelConfig(guildId);
            if (config == null)
            {
                return null;
            }

            if (config.Enabled)
            {
                var user = Database.Load<LevelUser>(LevelUser.DocumentName(userId, guildId));
                if (user == null)
                {
                    user = new LevelUser(userId, guildId);
                    Database.Store(user, LevelUser.DocumentName(userId, guildId));
                }

                return new Tuple<LevelUser, LevelConfig>(user, config);
            }

            return null;
        }

        public LevelConfig TryGetLevelConfig(ulong guildId)
        {
            var config = Database.Load<LevelConfig>(LevelConfig.DocumentName(guildId));
            return config;
        }

        public LevelConfig GetOrCreateLevelConfig(ulong guildId)
        {
            var config = TryGetLevelConfig(guildId);
            if (config == null)
            {
                config = new LevelConfig(guildId);
                Database.Store(config, LevelConfig.DocumentName(guildId));
            }
            return config;
        }

        public ConcurrentDictionary<ulong, List<ulong>> UsersUpdated = new ConcurrentDictionary<ulong, List<ulong>>();

        public async Task LevelEvent(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message))
            {
                return;
            }

            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }

            if (!(message.Channel is SocketTextChannel tChannel))
            {
                return;
            }

            if (tChannel.Guild == null)
            {
                return;
            }
            
            if (!LocalManagementService.LastConfig.IsAcceptable(tChannel.Guild.Id))
            {
                return;
            }
        
            if (UsersUpdated.TryGetValue(tChannel.Guild.Id, out var userList))
            {
                if (userList.Contains(message.Author.Id))
                {
                    return;
                }
                else
                {
                    userList.Add(message.Author.Id);
                }
            }
            else
            {
                UsersUpdated.TryAdd(tChannel.Guild.Id, new List<ulong>
                {
                    message.Author.Id
                });
            }
            var _ = Task.Run(async () =>
            {
                try
                {
                    var config = GetLevelUser(tChannel.Guild.Id, message.Author.Id);
                    if (config == null)
                    {
                        return;
                    }

                    var user = config.Item1;
                    var guild = config.Item2;

                    user.UserXP += 10;

                    var requiredXP = RequiredExp(user.UserLevel);
                    if (user.UserXP >= requiredXP)
                    {
                        user.UserLevel++;
                        Database.Store(user, LevelUser.DocumentName(user.UserId, user.GuildId));
                        if (guild.RewardRoles.Any())
                        {
                            var roles = GetRoles(user.UserLevel, guild, tChannel.Guild);
                            var gUser = tChannel.Guild.GetUser(message.Author.Id);
                            await gUser?.AddRolesAsync(roles);
                        }

                        var messageContent = $"**{message.Author.Mention} is now level {user.UserLevel}** XP: {user.UserXP} Next Level At {RequiredExp(user.UserLevel)} XP";
                        if (guild.ReplyLevelUps)
                        {
                            await tChannel.SendMessageAsync(messageContent);
                        }

                        if (guild.LogChannelId != 0)
                        {
                            var logChannel = tChannel.Guild.GetTextChannel(guild.LogChannelId);
                            if (logChannel != null)
                            {
                                await logChannel.SendMessageAsync(messageContent);
                            }
                        }
                    }
                    else
                    {
                        Database.Store(user, LevelUser.DocumentName(user.UserId, user.GuildId));
                    }
                }
                catch (Exception e)
                {
                    //
                }
            });

        }

        public List<SocketRole> GetRoles(int level, LevelConfig config, SocketGuild guild)
        {
            var roles = config.RewardRoles.Where(x => x.LevelRequirement < level).ToList();

            if (roles.Any())
            {
                var gRoles = roles.Select(x => guild.GetRole(x.RoleId)).Where(x => x != null).ToList();
                return gRoles;
            }

            return new List<SocketRole>();
        }

        public int RequiredExp(int level)
        {
            var requiredXP = (level * 50) + ((level * level) * 25);
            return requiredXP;
        }
    }
}