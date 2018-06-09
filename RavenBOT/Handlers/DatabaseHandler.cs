using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Discord;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenBOT.Models;

namespace RavenBOT.Handlers
{
    public class DBObject
    {
        public string Name = "RavenBOT";
        public bool IsConfigCreated;
        public string FullBackup = "0 */6 * * *";
        public string URL = "http://127.0.0.1:8080";
        public string IncrementalBackup = "0 2 * * *";
        public string BackupFolder => Directory.CreateDirectory("Backup").FullName;
    }

    public class DatabaseHandler
    {
        public static DBObject Settings { get; set; }
        public static IDocumentStore Store { get; set; }

        public bool Ping(string url)
        {
            try
            {
                var webClient = new WebClient();
                var _ = webClient.DownloadData(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Initialize()
        {
            LogHandler.PrintApplicationInformation();
            if (!File.Exists("setup/DBConfig.json"))
            {
                LogHandler.LogMessage("Enter the database Name: (ie. MyRavenDatabase)");
                var dbname = Console.ReadLine();
                LogHandler.LogMessage("Enter the database URL: (typically http://127.0.0.1:8080 if hosting locally)");
                var dburl = Console.ReadLine();
                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(new DBObject
                {
                    Name = dbname,
                    URL = dburl
                }, Formatting.Indented), Encoding.UTF8);

                Settings = JsonConvert.DeserializeObject<DBObject>(File.ReadAllText("setup/DBConfig.json"));
            }
            else
            {
                Settings = JsonConvert.DeserializeObject<DBObject>(File.ReadAllText("setup/DBConfig.json"));
            }

            Store = new Lazy<IDocumentStore>(() => new DocumentStore { Database = Settings.Name, Urls = new[] { Settings.URL } }.Initialize(), true).Value;
            if (Store == null) LogHandler.LogMessage("Failed to build document store.", LogSeverity.Critical);

            if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != Settings.Name))
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Settings.Name)));

            var Record = Store.Maintenance.Server.Send(new GetDatabaseRecordOperation(Settings.Name));
            var backupop = Record.PeriodicBackups.FirstOrDefault(x => x.Name == "Backup");
            try
            {
                if (backupop == null)
                {
                    var newbackup = new PeriodicBackupConfiguration
                    {
                        Name = "Backup",
                        BackupType = BackupType.Backup,
                        FullBackupFrequency = Settings.FullBackup,
                        IncrementalBackupFrequency = Settings.IncrementalBackup,
                        LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder }
                    };
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(newbackup));
                }
                else
                {
                    //In the case that we already have a backup operation setup, ensure that we update the backup location accordingly
                    backupop.LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder };
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(backupop));
                }
            }
            catch
            {
                LogHandler.LogMessage("RavenDB: Failed to set Backup Operation. Backups may not be saved", LogSeverity.Warning);
            }


            if (Settings.IsConfigCreated == false)
            {
                LogHandler.LogMessage("Enter bot's token: ");
                var Token = Console.ReadLine();
                LogHandler.LogMessage("Enter bot's prefix: ");
                var Prefix = Console.ReadLine();
                Execute<ConfigModel>(Operation.CREATE, new ConfigModel
                {
                    Prefix = Prefix,
                    Token = Token

                }, "Config");
                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(new DBObject { IsConfigCreated = true }, Formatting.Indented));
            }
            Settings = null;
        }

        /*
        public static async void DatabaseInitialise(DiscordSocketClient client)
        {



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
                FullBackupFrequency = "0 6 * * *", //REMEMBER TO CHANGE BACK
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
        */


        public List<T> Query<T>()
        {
            using (var session = Store.OpenSession())
            {
                List<T> QueriedItems;
                try
                {
                    QueriedItems = session.Query<T>().ToList();
                }
                catch
                {
                    QueriedItems = new List<T>();
                }

                return QueriedItems;
            }
        }

        public enum Operation
        {
            SAVE,
            LOAD,
            DELETE,
            CREATE
        }

        public T Execute<T>(Operation Operation, object Data = null, object Id = null) where T : class
        {
            using (var Session = Store.OpenSession(Store.Database))
            {
                switch (Operation)
                {
                    case Operation.CREATE:
                        if (Session.Advanced.Exists($"{Id}")) break;
                        Session.Store((T)Data, $"{Id}");
                        LogHandler.LogMessage($"RavenDB: Created => {typeof(T).Name} | ID: {Id}");
                        break;

                    case Operation.DELETE:
                        LogHandler.LogMessage($"RavenDB: Removed => {typeof(T).Name} | ID: {Id}");
                        Session.Delete(Session.Load<T>($"{Id}")); break;
                    case Operation.LOAD:
                        return Session.Load<T>($"{Id}");
                    case Operation.SAVE:
                        var Load = Session.Load<T>($"{Id}");
                        if (Session.Advanced.IsLoaded($"{Id}") == false || Load == Data) break;
                        Session.Advanced.Evict(Load);
                        Session.Store((T)Data, $"{Id}");
                        Session.SaveChanges();
                        break;
                }
                if (Operation == Operation.CREATE || Operation == Operation.DELETE) Session.SaveChanges();
                Session.Dispose();
            }
            return null;
        }
    }
}