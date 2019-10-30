using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;

namespace RavenBOT.Common.Interfaces.Database
{
    public class RavenDatabase : IDatabase
    {
        public class RavenDatabaseConfig
        {
            public string DatabaseName { get; set; }

            public List<string> DatabaseUrls { get; set; } = new List<string>();

            public string CertificatePath { get; set; } = null;
        }

        public IDocumentStore DocumentStore { get; set; }

        public void StartServer(RavenDatabaseConfig config)
        {
            try
            {
                if (config.CertificatePath != null && File.Exists(config.CertificatePath))
                {
                    DocumentStore = new DocumentStore
                    {
                        Database = config.DatabaseName,
                        Urls = config.DatabaseUrls.ToArray(),
                        Certificate = new X509Certificate2(X509Certificate.CreateFromCertFile(config.CertificatePath))
                    }.Initialize();
                }
                else
                {
                    DocumentStore = new DocumentStore
                    {
                        Database = config.DatabaseName,
                        Urls = config.DatabaseUrls.ToArray()
                    }.Initialize();
                }

                if (DocumentStore.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 25)).All(x => !x.Equals(config.DatabaseName)))
                {
                    DocumentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(config.DatabaseName)));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            DocumentStore.Initialize();
        }

        public RavenDatabase(RavenDatabaseConfig config)
        {
            StartServer(config);
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