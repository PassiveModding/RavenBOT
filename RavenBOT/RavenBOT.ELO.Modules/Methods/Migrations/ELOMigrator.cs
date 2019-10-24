using System;
using System.Linq;
using ELO.Models;
using RavenBOT.Common.Interfaces.Database;
using RavenBOT.ELO.Modules.Models;
using RavenBOT.ELO.Modules.Modules;
using RavenBOT.ELO.Modules.Premium;

namespace RavenBOT.ELO.Modules.Methods.Migrations
{
    public class ELOMigrator
    {
        private LiteDataStore currentDatabase;
        private RavenDatabase oldDatabase;

        public ELOMigrator(string configPath, LiteDataStore currentDatabase, LegacyIntegration legacy)
        {
            //This has been written with the face that old database is RDB and current is LiteDB.
            this.oldDatabase = new RavenDatabase();
            RavenDatabase.ConfigPath = configPath;
            this.currentDatabase = currentDatabase;
            Legacy = legacy;
        }

        public LegacyIntegration Legacy { get; }

        public void RunMigration()
        {
            var configs = oldDatabase.Query<GuildModel>();
            foreach (var config in configs)
            {
                try
                {
                    //Do not use if new competition already exists
                    var newComp = currentDatabase.Load<CompetitionConfig>(CompetitionConfig.DocumentName(config.ID));
                    if (newComp != null) continue;

                    //Do not set this due to incompatibilities with new replacements
                    //newComp.NameFormat = config.Settings.Registration.NameFormat;
                    newComp.UpdateNames = true;
                    newComp.RegisterMessageTemplate = config.Settings.Registration.Message;
                    newComp.RegisteredRankId = config.Ranks.FirstOrDefault(x => x.IsDefault)?.RoleID ?? 0;
                    newComp.Ranks = config.Ranks.Select(x => new Rank
                    {
                        RoleId = x.RoleID,
                        WinModifier = x.WinModifier,
                        LossModifier = x.LossModifier,
                        Points = x.Threshold
                    }).ToList();
                    newComp.GuildId = config.ID;
                    newComp.DefaultWinModifier = config.Settings.Registration.DefaultWinModifier;
                    newComp.DefaultLossModifier = config.Settings.Registration.DefaultLossModifier;
                    newComp.AllowReRegister = config.Settings.Registration.AllowMultiRegistration;
                    newComp.AllowSelfRename = config.Settings.Registration.AllowMultiRegistration;
                    newComp.AllowNegativeScore = config.Settings.GameSettings.AllowNegativeScore;
                    newComp.BlockMultiQueueing = config.Settings.GameSettings.BlockMultiQueuing;
                    newComp.AdminRole = config.Settings.Moderation.AdminRoles.FirstOrDefault();
                    newComp.ModeratorRole = config.Settings.Moderation.ModRoles.FirstOrDefault();
                    //TODO: Remove user on afk   
                    
                    if (config.Settings.Premium.Expiry > DateTime.UtcNow)
                    {
                        Legacy.SaveConfig(new LegacyIntegration.LegacyPremium
                        {
                            GuildId = config.ID,
                            ExpiryDate = config.Settings.Premium.Expiry
                        });
                    }

                    currentDatabase.Store(newComp, CompetitionConfig.DocumentName(config.ID));
                    
                    foreach (var lobby in config.Lobbies)
                    {
                        var newLobby = new Lobby();
                        newLobby.GuildId = config.ID;
                        newLobby.ChannelId = lobby.ChannelID;
                        newLobby.DmUsersOnGameReady = config.Settings.GameSettings.DMAnnouncements;
                        newLobby.GameResultAnnouncementChannel = config.Settings.GameSettings.AnnouncementsChannel;
                        newLobby.GameReadyAnnouncementChannel = config.Settings.GameSettings.AnnouncementsChannel;
                        newLobby.PlayersPerTeam = lobby.UserLimit/2;
                        newLobby.Description = lobby.Description;
                        //TODO: Lobby requeue delay      

                        currentDatabase.Store(newLobby, Lobby.DocumentName(config.ID, lobby.ChannelID));
                    }

                    foreach (var user in config.Users)
                    {
                        var newUser = new Player(user.UserID, config.ID, user.Username);
                        newUser.Points = user.Stats.Points;
                        newUser.Wins = user.Stats.Wins;
                        newUser.Losses = user.Stats.Losses;
                        newUser.Draws = user.Stats.Draws;

                        //TODO: Kills/Deaths/Assists

                        if (user.Banned != null && user.Banned.Banned)
                        {
                            var length = user.Banned.ExpiryTime - DateTime.UtcNow;
                            newUser.BanHistory.Add(new Player.Ban(length, user.Banned.Moderator, user.Banned.Reason));
                        }

                        currentDatabase.Store(newUser, Player.DocumentName(config.ID, user.UserID));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}