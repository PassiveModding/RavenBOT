using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LiteDB;

namespace RavenBOT.Services.Database
{
    public class LiteDataStore : IDatabase
    {
        public string DatabaseFolder = Path.Combine(AppContext.BaseDirectory, "LiteDB");

        public LiteDatabase Database { get; }
        public LiteDataStore()
        {
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
            }
            Database = new LiteDatabase(Path.Combine(DatabaseFolder, "LiteDB.db"));
        }

        public void Store<T>(T document, string name = null)
        {
            var collection = Database.GetCollection<T>();
            StoreInCollection(collection, document, name);
        }

        public void StoreInCollection<T>(LiteCollection<T> collection, T document, string name = null)
        {
            if (name != null)
            {
                collection.Upsert(new BsonValue(name), document);
            }
            else
            {
                collection.Insert(new BsonValue(DateTime.UtcNow), document);
            }
        }

        public void StoreMany<T>(List<T> documents, Func<T, string> docName = null)
        {
            var collection = Database.GetCollection<T>();

            foreach (var document in documents)
            {
                if (docName != null)
                {
                    StoreInCollection(collection, document, docName(document));
                }
                else
                {
                    StoreInCollection(collection, document);
                }
            }
        }

        public T Load<T>(string documentName)
        {
            var collection = Database.GetCollection<T>();
            try
            {
                var doc = collection.FindById(new BsonValue(documentName));
                return doc;
            }
            catch
            {
                return default(T);
            }
        }

        public List<T> Query<T>()
        {
            var collection = Database.GetCollection<T>();
            return collection.FindAll().ToList();
        }

        public void RemoveDocument<T>(T document)
        {
            var collection = Database.GetCollection<T>();
            collection.Delete(x => x.Equals(document));
        }

        public void Remove<T>(string documentName)
        {
            var collection = Database.GetCollection<T>();
            collection.Delete(new BsonValue(documentName));
        }
    }
}
