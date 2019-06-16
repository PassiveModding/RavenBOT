using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Embedded;

namespace RavenBOT.Common.Interfaces.Database
{
    public class RavenDBEmbedded : IDatabase
    {
        public class RavenEmbeddedConfig
        {
            public string DatabaseName { get; set; }
            //public string CertificatePath {get;set;} = null;

            public int Port { get; set; } = 8080;
        }

        private static readonly string ConfigDirectory = Path.Combine(AppContext.BaseDirectory, "setup");
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "RavenEmbeddedConfig.json");

        private IDocumentStore DocumentStore { get; }

        public RavenDBEmbedded()
        {
            var config = GetOrCreateConfig();

            var serverOptions = new ServerOptions()
            {
                //NOTE: This requires the runtime framework version to be set in the csproj when compiling
                FrameworkVersion = "2.1.6",
                ServerUrl = $"http://127.0.0.1:{config.Port}"
            };

            EmbeddedServer.Instance.StartServer(serverOptions);
            DocumentStore = EmbeddedServer.Instance.GetDocumentStore(config.DatabaseName);

            Console.WriteLine($"RavenDB Server Url: {serverOptions.ServerUrl}");
        }

        public RavenEmbeddedConfig GetOrCreateConfig()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            if (!File.Exists(ConfigPath))
            {
                var newConfig = new RavenEmbeddedConfig();
                Console.WriteLine("Please input your database name (DEFAULT: RavenBOT)");
                var databaseName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    databaseName = "RavenBOT";
                }

                newConfig.DatabaseName = databaseName;

                Console.WriteLine("Please input desired studio port (DEFAULT: 8080)");
                var port = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(port))
                {
                    port = "8080";
                }
                newConfig.Port = int.Parse(port);

                /*Console.WriteLine("Please input your certificate path (leave empty if unauthenticated.)");
                var path = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    databaseName = null;
                }
                newConfig.CertificatePath = path;
                */

                Console.WriteLine($"New Config Created! It can be found at \"{ConfigPath}\" please delete or edit it if you wish to modify settings");

                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(newConfig, Formatting.Indented));
                return newConfig;
            }

            return JsonConvert.DeserializeObject<RavenEmbeddedConfig>(File.ReadAllText(ConfigPath));
        }

        public void Store<T>(T document, string name = null)
        {
            using(var session = DocumentStore.OpenSession())
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
            using(var session = DocumentStore.OpenSession())
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
            using(var session = DocumentStore.OpenSession())
            {
                var document = session.Load<T>(documentName);
                return document;
            }
        }

        public IEnumerable<T> Query<T>()
        {
            using(var session = DocumentStore.OpenSession())
            {
                return session.Query<T>().ToList();
            }
        }

        public void RemoveDocument<T>(T document)
        {
            using(var session = DocumentStore.OpenSession())
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
            using(var session = DocumentStore.OpenSession())
            {
                session.Delete(documentName);
                session.SaveChanges();
            }
        }

        public void RemoveManyDocuments<T>(List<T> documents)
        {
            using(var session = DocumentStore.OpenSession())
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
            using(var session = DocumentStore.OpenSession())
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
            using(var session = DocumentStore.OpenSession())
            {
                return session.Advanced.Exists(docName);
            }
        }
    }
}