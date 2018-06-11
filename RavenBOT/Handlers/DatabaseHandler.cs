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

    /// <summary>
    /// The database handler.
    /// </summary>
    public class DatabaseHandler
    {
        /// <summary>
        /// The operation.
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Saves the a document
            /// </summary>
            SAVE,

            /// <summary>
            /// Loads a document
            /// </summary>
            LOAD,

            /// <summary>
            /// Deletes a document
            /// </summary>
            DELETE,

            /// <summary>
            /// Adds a new document
            /// </summary>
            CREATE
        }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public DatabaseObject Settings { get; set; }

        /// <summary>
        /// Gets or sets the store.
        /// </summary>
        public static IDocumentStore Store { get; set; }

        /// <summary>
        /// Pings a web url
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Ping(string url)
        {
            try
            {
                var webClient = new WebClient();
                var discarded = webClient.DownloadData(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Initialize()
        {
            //Ensure that the bot's database settings are setup, if not prompt to enter details
            if (!File.Exists("setup/DBConfig.json"))
            {
                LogHandler.LogMessage("Please enter details about your bot and database configuration. NOTE: You can hit enter for a default value. ");
                LogHandler.LogMessage("Enter the database Name: (ie. MyRavenDatabase) DEFAULT: RavenBOT");
                var dbname = Console.ReadLine();
                if (string.IsNullOrEmpty(dbname))
                {
                    dbname = "RavenBOT";
                }

                LogHandler.LogMessage("Enter the database URL: (typically http://127.0.0.1:8080 if hosting locally) DEFAULT: http://127.0.0.1:8080");
                var dburl = Console.ReadLine();
                if (string.IsNullOrEmpty(dburl))
                {
                    dburl = "http://127.0.0.1:8080";
                }

                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject
                {
                    Name = dbname,
                    URL = dburl
                }, Formatting.Indented), Encoding.UTF8);

                Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("setup/DBConfig.json"));
            }
            else
            {
                Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("setup/DBConfig.json"));
            }

            //This initialises the document store, and ensures that RavenDB is working properly
            Store = new Lazy<IDocumentStore>(() => new DocumentStore {Database = Settings.Name, Urls = new[] {Settings.URL}}.Initialize(), true).Value;
            if (Store == null)
            {
                LogHandler.LogMessage("Failed to build document store.", LogSeverity.Critical);
            }

            //This creates the database
            if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != Settings.Name))
            {
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Settings.Name)));
            }

            //To ensure the backup operation is functioning and backing up to our bot's directory we update the backup operation on each boot of the bot
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
                        LocalSettings = new LocalSettings {FolderPath = Settings.BackupFolder}
                    };
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(newbackup));
                }
                else
                {
                    //In the case that we already have a backup operation setup, ensure that we update the backup location accordingly
                    backupop.LocalSettings = new LocalSettings {FolderPath = Settings.BackupFolder};
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(backupop));
                }
            }
            catch
            {
                LogHandler.LogMessage("RavenDB: Failed to set Backup Operation. Backups may not be saved", LogSeverity.Warning);
            }

            var Cmodel = new ConfigModel();
            //Prompt the user to set up the bot's configuration.
            if (Settings.IsConfigCreated == false)
            {
                LogHandler.LogMessage("Enter bot's token: (You can get this from https://discordapp.com/developers/applications/me)");
                var Token = Console.ReadLine();
                if (string.IsNullOrEmpty(Token))
                {
                    throw new Exception("You must supply a token for this bot to operate.");
                }

                LogHandler.LogMessage("Enter bot's prefix: (This will be used to initiate a command, ie. +say or +help) DEFAULT: +");
                var Prefix = Console.ReadLine();
                if (string.IsNullOrEmpty(Prefix))
                {
                    Prefix = "+";
                }

                Cmodel.Token = Token;
                Cmodel.Prefix = Prefix;

                //This inserts the config object into the database and writes the DatabaseConfig to file.
                Execute<ConfigModel>(Operation.CREATE, Cmodel, "Config");
                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject {IsConfigCreated = true}, Formatting.Indented));
            }

            LogHandler.PrintApplicationInformation(Settings, Cmodel);
            Settings = null;
        }

        //RavenDb allows the user to query all objects in the database based on their Object Type.
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

        public T Execute<T>(Operation Operation, object Data = null, object Id = null) where T : class
        {
            using (var Session = Store.OpenSession(Store.Database))
            {
                switch (Operation)
                {
                    case Operation.CREATE:
                        if (Session.Advanced.Exists($"{Id}")) break;
                        Session.Store((T) Data, $"{Id}");
                        LogHandler.LogMessage($"RavenDB: Created => {typeof(T).Name} | ID: {Id}");
                        break;

                    case Operation.DELETE:
                        LogHandler.LogMessage($"RavenDB: Removed => {typeof(T).Name} | ID: {Id}");
                        Session.Delete(Session.Load<T>($"{Id}"));
                        break;
                    case Operation.LOAD:
                        return Session.Load<T>($"{Id}");
                    case Operation.SAVE:
                        var Load = Session.Load<T>($"{Id}");
                        if (Session.Advanced.IsLoaded($"{Id}") == false || Load == Data) break;
                        Session.Advanced.Evict(Load);
                        Session.Store((T) Data, $"{Id}");
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