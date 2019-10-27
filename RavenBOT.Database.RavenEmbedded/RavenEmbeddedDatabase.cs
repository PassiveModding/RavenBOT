using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Embedded;

namespace RavenBOT.Common.Interfaces.Database
{
    public class RavenEmbeddedDatabase : IDatabase
    {
        public class RavenEmbeddedConfig
        {
            public string DatabaseName { get; set; }

            public List<string> DatabaseUrls { get; set; } = new List<string>();

            public string CertificatePath { get; set; } = null;
        }

        public IDocumentStore DocumentStore { get; set; }
        public LocalManagementService LocalManagementService { get; }

        public string ConfigKey = "RavenEmbeddedConfig";

        public void StartEmbeddedServer(RavenEmbeddedConfig config)
        {
            if (config.DatabaseUrls.Count > 1)
            {
                Console.WriteLine($"Embedded database defaulting to 127.0.0.1:8080");
            }

            var serverOptions = new ServerOptions()
            {
                //NOTE: This requires the runtime framework version to be set in the csproj when compiling
                FrameworkVersion = "2.1.6",
                ServerUrl = "http://127.0.0.1:8080"
            };

            EmbeddedServer.Instance.StartServer(serverOptions);
            DocumentStore = EmbeddedServer.Instance.GetDocumentStore(config.DatabaseName);

            Console.WriteLine($"RavenDB Server Url: {serverOptions.ServerUrl}");
        }

        public RavenEmbeddedDatabase(LocalManagementService localManagementService)
        {
            LocalManagementService = localManagementService;
            StartEmbeddedServer(GetOrInitializeConfig());
        }

        public RavenEmbeddedConfig GetOrInitializeConfig()
        {
            var localConfig = LocalManagementService.GetConfig();
            if (localConfig.AdditionalConfigs.ContainsKey(ConfigKey))
            {
                return localConfig.GetConfig<RavenEmbeddedConfig>(ConfigKey); ;
            }
            else
            {
                var newConfig = new RavenEmbeddedConfig();
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
                newConfig.CertificatePath = Console.ReadLine();

                Console.WriteLine($"New Config Created! It can be found at \"{LocalManagementService.ConfigPath}\" under AdditionalConfigs[{ConfigKey}] please delete or edit it if you wish to modify the database url or name");

                localConfig.AdditionalConfigs.Add(ConfigKey, newConfig);
                LocalManagementService.SaveConfig(localConfig);
                return newConfig;
            }
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
                        var name = docName(document);
                        session.Store(document, name);
                    }
                }

                session.SaveChanges();
            }
        }

        public T Load<T>(string documentName) where T : class
        {
            using (var session = DocumentStore.OpenSession())
            {
                var document = session.Load<T>(documentName);
                return document;
            }
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> queryFunc)
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Query<T>().Where(queryFunc).ToList();
            }
        }

        public IEnumerable<T> Query<T>()
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Query<T>().ToList();
            }
        }

        //TODO: Unsure if document deletion will work with document referenced outside the session?
        public void RemoveDocument<T>(T document)
        {
            using (var session = DocumentStore.OpenSession())
            {
                try
                {
                    session.Delete(document);
                    session.SaveChanges();
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void Remove<T>(string documentName)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Delete(documentName);
                session.SaveChanges();
            }
        }

        public void RemoveManyDocuments<T>(List<T> documents)
        {
            using (var session = DocumentStore.OpenSession())
            {
                foreach (var document in documents)
                {
                    session.Delete(document);
                }

                session.SaveChanges();
            }
        }

        public void RemoveMany<T>(List<string> docNames)
        {
            using (var session = DocumentStore.OpenSession())
            {
                foreach (var name in docNames)
                {
                    session.Delete(name);
                }

                session.SaveChanges();
            }
        }

        public bool Exists<T>(string docName)
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Advanced.Exists(docName);
            }
        }

        public bool Any<T>(Expression<Func<T, bool>> queryFunc)
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Query<T>().Any(queryFunc);
            }
        }
    }
}