using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ahd.Graphite;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenBOT.Models;

namespace RavenBOT.Services.Database
{
    public class RavenDatabase : IDatabase
    {
        private IDocumentStore DocumentStore { get; }
        private GraphiteClient Graphite { get; }
        private DatabaseConfig Config { get; }
        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "setup");
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "Config.json");

        public RavenDatabase()
        {
            Config = GetOrInitializeConfig();

            try
            {
                if (Config.pathToCertificate != null && File.Exists(Config.pathToCertificate))
                {
                    DocumentStore = new DocumentStore
                                {
                                    Database = Config.DatabaseName,
                                    Urls = Config.DatabaseUrls.ToArray(),
                                    Certificate = new X509Certificate2(X509Certificate.CreateFromCertFile(Config.pathToCertificate))
                                }.Initialize();
                }
                else
                {
                    DocumentStore = new DocumentStore
                                {
                                    Database = Config.DatabaseName,
                                    Urls = Config.DatabaseUrls.ToArray()
                                }.Initialize();
                }


                if (DocumentStore.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => !x.Equals(Config.DatabaseName)))
                {
                    DocumentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(Config.DatabaseName)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            try
            {
                if (Config.GraphiteUrl != null)
                {
                    Graphite = new GraphiteClient(Config.GraphiteUrl);
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool UsingGraphite()
        {
            return Graphite != null;
        }

        public GraphiteClient GetGraphiteClient()
        {
            //Remember, this can return null
            return Graphite;
        }

        public DatabaseConfig GetOrInitializeConfig()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            if (!File.Exists(ConfigPath))
            {
                var newConfig = new DatabaseConfig();
                Console.WriteLine("Please input your database name (DEFAULT: RavenBOT)");
                var databaseName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    databaseName = "RavenBOT";
                }

                newConfig.DatabaseName = databaseName;
                Console.WriteLine("Please input the url to your RavenDB instance (DEFAULT: http://127.0.0.1:8080)");
                var databaseUrl = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(databaseUrl))
                {
                    databaseUrl = "http://127.0.0.1:8080";
                }

                newConfig.DatabaseUrls = new List<string>
                                             {
                                                 databaseUrl
                                             };

                Console.WriteLine("Please input the path to your certificate for the database (Leave blank if you are using an unauthenticated deployment of ravendb)");
                newConfig.pathToCertificate = Console.ReadLine();

                Console.WriteLine("Please input the url of your Graphite instance (Put NONE if you do not have one)");
                var graphiteUrl = Console.ReadLine();
                newConfig.GraphiteUrl = !graphiteUrl.Equals("none", StringComparison.InvariantCultureIgnoreCase) ? graphiteUrl : null;

                Console.WriteLine($"New Config Created! It can be found at \"{ConfigPath}\" please delete or edit it if you wish to modify the database url or name");

                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(newConfig, Formatting.Indented));
                return newConfig;
            }

            return JsonConvert.DeserializeObject<DatabaseConfig>(File.ReadAllText(ConfigPath));
        }


        public void Store<T>(T document, string name = null)
        {
            using (var session = DocumentStore.OpenSession())
            {
                if (name == null)
                {
                    session.Store(document);
                }
                else
                {
                    session.Store(document, name);
                }

                session.SaveChanges();
            }
        }

        public void StoreMany<T>(List<T> documents, Func<T, string> docName = null)
        {
            using (var session = DocumentStore.OpenSession())
            {
                if (docName == null)
                {
                    foreach (var document in documents)
                    {
                        session.Store(document);
                    }
                }
                else
                {
                    foreach (var document in documents)
                    {
                        session.Store(document, docName(document));
                    }
                }

                session.SaveChanges();
            }
        }

        public T Load<T>(string documentName)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var document = session.Load<T>(documentName);
                return document;
            }
        }

        public List<T> Query<T>()
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Query<T>().ToList();
            }
        }
    }
}
