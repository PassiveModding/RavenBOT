namespace RavenBOT.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;

    using global::Discord;

    using Newtonsoft.Json;

    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Backups;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    using RavenBOT.Models;

    using Serilog;

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
        /// Gets or sets the store.
        /// </summary>
        public static IDocumentStore Store { get; set; }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public DatabaseObject Settings { get; set; }

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
                var unused = webClient.DownloadData(url);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initializes the database for use
        /// </summary>
        /// <returns>
        /// The <see cref="DatabaseObject"/>.
        /// </returns>
        public DatabaseObject Initialize()
        {
            // Ensure that the bots database settings are setup, if not prompt to enter details
            if (!File.Exists("setup/DBConfig.json"))
            {
                LogHandler.LogMessage("Please enter details about your bot and database configuration. NOTE: You can hit enter for a default value. ");
                LogHandler.LogMessage("Enter the database Name: (ie. MyRavenDatabase) DEFAULT: RavenBOT");
                var databaseName = Console.ReadLine();
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = "RavenBOT";
                }

                LogHandler.LogMessage("Enter the database URL: (typically http://127.0.0.1:8080 if hosting locally) DEFAULT: http://127.0.0.1:8080");
                var databaseUrl = Console.ReadLine();
                if (string.IsNullOrEmpty(databaseUrl))
                {
                    databaseUrl = "http://127.0.0.1:8080";
                }

                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(new DatabaseObject
                {
                    Name = databaseName,
                    URL = databaseUrl
                }, Formatting.Indented), Encoding.UTF8);

                Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("setup/DBConfig.json"));
            }
            else
            {
                Settings = JsonConvert.DeserializeObject<DatabaseObject>(File.ReadAllText("setup/DBConfig.json"));
            }

            // This initializes the document store, and ensures that RavenDB is working properly
            Store = new Lazy<IDocumentStore>(() => new DocumentStore { Database = Settings.Name, Urls = new[] { Settings.URL } }.Initialize(), true).Value;
            if (Store == null)
            {
                LogHandler.LogMessage("Failed to build document store.", LogSeverity.Critical);
            }

            // This creates the database
            if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != Settings.Name))
            {
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Settings.Name)));
            }

            // To ensure the backup operation is functioning and backing up to our bots directory we update the backup operation on each boot of the bot
            var record = Store.Maintenance.Server.Send(new GetDatabaseRecordOperation(Settings.Name));
            var backup = record.PeriodicBackups.FirstOrDefault(x => x.Name == "Backup");
            try
            {
                if (backup == null)
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
                    // In the case that we already have a backup operation setup, ensure that we update the backup location accordingly
                    backup.LocalSettings = new LocalSettings { FolderPath = Settings.BackupFolder };
                    Store.Maintenance.ForDatabase(Settings.Name).Send(new UpdatePeriodicBackupOperation(backup));
                }
            }
            catch
            {
                LogHandler.LogMessage("RavenDB: Failed to set Backup operation. Backups may not be saved", LogSeverity.Warning);
            }

            var configModel = new ConfigModel();

            // Prompt the user to set up the bots configuration.
            if (Settings.IsConfigCreated == false)
            {
                LogHandler.LogMessage("Enter bots token: (You can get this from https://discordapp.com/developers/applications/me)");
                var token = Console.ReadLine();
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("You must supply a token for this bot to operate.");
                }

                LogHandler.LogMessage("Enter bots prefix: (This will be used to initiate a command, ie. +say or +help) DEFAULT: +");
                var prefix = Console.ReadLine();
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = "+";
                }

                configModel.Token = token;
                configModel.Prefix = prefix;

                // This inserts the config object into the database and writes the DatabaseConfig to file.
                Execute<ConfigModel>(Operation.CREATE, configModel, "Config");
                Settings.IsConfigCreated = true;
                File.WriteAllText("setup/DBConfig.json", JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            else
            {
                configModel = Execute<ConfigModel>(Operation.LOAD, null, "Config");
            }

            LogHandler.PrintApplicationInformation(Settings, configModel);
            LogHandler.Log = new LoggerConfiguration()
                .WriteTo
                .Console()
                .WriteTo
                .RavenDB(Store)
                .CreateLogger();
            return Settings;
        }

        /// <summary>
        /// RavenDb allows the user to query all objects in the database based on their Object Type.
        /// </summary>
        /// <typeparam name="T">
        /// Object Type to query
        /// </typeparam>
        /// <returns>
        /// List of the queried objects
        /// </returns>
        public List<T> Query<T>()
        {
            using (var session = Store.OpenSession())
            {
                List<T> queriedItems;
                try
                {
                    queriedItems = session.Query<T>().ToList();
                }
                catch
                {
                    queriedItems = new List<T>();
                }

                return queriedItems;
            }
        }

        /// <summary>
        /// Gets or Posts to the database
        /// </summary>
        /// <typeparam name="T">
        /// The Object's Type being queried
        /// </typeparam>
        /// <param name="operation">
        /// The operation.
        /// </param>
        /// <param name="data">
        /// The object to save (if applicable)
        /// </param>
        /// <param name="id">
        /// The unique name of the object
        /// </param>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        public T Execute<T>(Operation operation, object data = null, object id = null) where T : class
        {
            using (var session = Store.OpenSession(Store.Database))
            {
                switch (operation)
                {
                    case Operation.CREATE:
                        if (session.Advanced.Exists($"{id}"))
                        {
                            break;
                        }

                        session.Store((T)data, $"{id}");
                        LogHandler.LogMessage($"RavenDB: Created => {typeof(T).Name} | ID: {id}");
                        break;

                    case Operation.DELETE:
                        LogHandler.LogMessage($"RavenDB: Removed => {typeof(T).Name} | ID: {id}");
                        session.Delete(session.Load<T>($"{id}"));
                        break;
                    case Operation.LOAD:
                        return session.Load<T>($"{id}");
                    case Operation.SAVE:
                        var load = session.Load<T>($"{id}");
                        if (session.Advanced.IsLoaded($"{id}") == false || load == data)
                        {
                            break;
                        }

                        session.Advanced.Evict(load);
                        session.Store((T)data, $"{id}");
                        session.SaveChanges();
                        break;
                }

                if (operation == Operation.CREATE || operation == Operation.DELETE)
                {
                    session.SaveChanges();
                }

                session.Dispose();
            }

            return null;
        }
    }
}