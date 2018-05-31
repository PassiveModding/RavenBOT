using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenBOT.Models;

namespace RavenBOT.Handlers
{
    public class DatabaseHandler
    {
        public DatabaseHandler(IDocumentStore store)
        {
            Store = store;
        }

        /// <summary>
        ///     This is the document store, an interface that represents our database
        /// </summary>
        public static IDocumentStore Store { get; set; }

        /// <summary>
        ///     Check whether RavenDB is running
        ///     Check whether or not a database already exists with the DBName
        ///     Set up auto-backup of the database
        ///     Ensure that all guilds shared with the bot have been added to the database
        /// </summary>
        /// <param name="client"></param>
        public static async void DatabaseInitialise(DiscordSocketClient client)
        {
            if (Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "Raven.Server") == null)
            {
                LogHandler.LogMessage("RavenDB: Server isn't running. Please make sure RavenDB is running.\n" +
                                      "Exiting ...", LogSeverity.Critical);
                await Task.Delay(5000);
                Environment.Exit(Environment.ExitCode);
            }

            var dbcreated = false;
            if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != CommandHandler.Config.DBName))
            {
                await Store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(CommandHandler.Config.DBName)));
                LogHandler.LogMessage($"Created Database {CommandHandler.Config.DBName}.");
                dbcreated = true;
            }


            LogHandler.LogMessage("Setting up backup operation...");
            var newbackup = new PeriodicBackupConfiguration
            {
                Name = "Backup",
                BackupType = BackupType.Backup,
                //Backup every 6 hours
                FullBackupFrequency = "0 */6 * * *",
                IncrementalBackupFrequency = "0 2 * * *",
                LocalSettings = new LocalSettings { FolderPath = Path.Combine(AppContext.BaseDirectory, "setup/backups/") }
            };
            var Record = Store.Maintenance.ForDatabase(CommandHandler.Config.DBName).Server.Send(new GetDatabaseRecordOperation(CommandHandler.Config.DBName));
            var backupop = Record.PeriodicBackups.FirstOrDefault(x => x.Name == "Backup");
            if (backupop == null)
            {
                await Store.Maintenance.ForDatabase(CommandHandler.Config.DBName).SendAsync(new UpdatePeriodicBackupOperation(newbackup)).ConfigureAwait(false);
            }
            else
            {
                //In the case that we already have a backup operation setup, ensure that we update the backup location accordingly
                backupop.LocalSettings = new LocalSettings { FolderPath = Path.Combine(AppContext.BaseDirectory, "setup/backups/") };
                await Store.Maintenance.ForDatabase(CommandHandler.Config.DBName).SendAsync(new UpdatePeriodicBackupOperation(backupop));
            }

            if (!dbcreated) return;

            using (var session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                try
                {
                    //Check to see wether or not we can actually load the Guilds List saved in our RavenDB
                    var _ = session.Query<GuildModel>().ToList();
                }
                catch
                {
                    //In the case that the check fails, ensure we initalise all servers that contain the bot.
                    var glist = client.Guilds.Select(x => new GuildModel
                    {
                        ID = x.Id
                    }).ToList();
                    foreach (var gobj in glist)
                    {
                        session.Store(gobj, gobj.ID.ToString());
                    }

                    session.SaveChanges();
                }
            }
        }


        /// <summary>
        ///     This adds a new guild to the RavenDB
        /// </summary>
        /// <param name="Id">The Server's ID</param>
        public static void AddGuild(ulong Id)
        {
            using (var Session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                if (Session.Advanced.Exists($"{Id}")) return;
                Session.Store(new GuildModel
                {
                    ID = Id
                }, Id.ToString());
                Session.SaveChanges();
            }

            LogHandler.LogMessage($"Added Server With Id: {Id}", LogSeverity.Debug);
        }

        /// <summary>
        ///     This adds a new guild to the RavenDB
        /// </summary>
        /// <param name="Gmodel"></param>
        public static void InsertGuildObject(GuildModel Gmodel)
        {
            using (var Session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                if (Session.Advanced.Exists($"{Gmodel.ID}")) return;
                Session.Store(Gmodel, $"{Gmodel.ID}");
                Session.SaveChanges();
            }

            LogHandler.LogMessage($"Inserted Guild with ID: {Gmodel.ID}", LogSeverity.Debug);
        }

        /// <summary>
        ///     Remove a guild's config completely from the database
        /// </summary>
        /// <param name="Id"></param>
        public static void RemoveGuild(ulong Id)
        {
            using (var Session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                Session.Delete($"{Id}");
                Session.SaveChanges();
            }

            LogHandler.LogMessage($"Removed Server With Id: {Id}", LogSeverity.Debug);
        }

        /// <summary>
        ///     Load a Guild Object from the database
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static GuildModel GetGuild(ulong Id)
        {
            using (var Session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                return Session.Load<GuildModel>(Id.ToString());
            }
        }

        /// <summary>
        ///     Load all documents matching GuildModel from the database
        /// </summary>
        /// <returns></returns>
        public static List<GuildModel> GetFullConfig()
        {
            using (var session = Store.OpenSession(CommandHandler.Config.DBName))
            {
                List<GuildModel> dbGuilds;
                try
                {
                    dbGuilds = session.Query<GuildModel>().ToList();
                }
                catch
                {
                    dbGuilds = new List<GuildModel>();
                }

                return dbGuilds;
            }
        }
    }
}