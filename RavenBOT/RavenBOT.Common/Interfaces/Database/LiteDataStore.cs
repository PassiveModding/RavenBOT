using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;

namespace RavenBOT.Common.Interfaces.Database
{
    public class BaseEntity<T>
    {
        public BaseEntity(T value, string id = null)
        {
            Id = id ?? DateTime.UtcNow.Ticks.ToString();
            Value = value;
        }

        public BaseEntity() {}

        public string Id { get; set; }
        public T Value { get; set; }
    }

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
            var collection = GetCollection<T>();
            StoreInCollection(collection, document, name);
        }

        public void StoreInCollection<T>(LiteCollection<BaseEntity<T>> collection, T document, string name = null)
        {
            collection.Upsert(new BaseEntity<T>(document, name));
        }

        public void StoreMany<T>(List<T> documents, Func<T, string> docName = null)
        {
            var collection = GetCollection<T>();

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
            var collection = GetCollection<T>();
            try
            {
                var doc = collection.FindOne(x => x.Id == documentName);
                return doc.Value;
            }
            catch
            {
                return default(T);
            }
        }

        public IEnumerable<T> Query<T>(Func<T, bool> queryFunc)
        {
            var collection = GetCollection<T>();
            var all = collection.Find(x => queryFunc(x.Value));
            return all.Select(x => x.Value);
        }

        public IEnumerable<T> Query<T>()
        {
            var collection = GetCollection<T>();
            var all = collection.FindAll();
            return all.Select(x => x.Value);
        }

        public void RemoveDocument<T>(T document)
        {
            var collection = GetCollection<T>();
            collection.Delete(x => x.Equals(document));
        }

        private LiteCollection<BaseEntity<T>> GetCollection<T>()
        {
            var collection = Database.GetCollection<BaseEntity<T>>(typeof(T).Name);
            return collection;
        }

        public void Remove<T>(string documentName)
        {
            var collection = GetCollection<T>();
            collection.Delete(x => x.Id == documentName);
        }

        public void RemoveManyDocuments<T>(List<T> documents)
        {
            var collection = GetCollection<T>();
            collection.Delete(x => documents.Contains(x.Value));
        }

        public void RemoveMany<T>(List<string> docNames)
        {
            var collection = GetCollection<T>();
            foreach (var name in docNames)
            {
                collection.Delete(x => x.Id == name);
            }
        }

        public bool Exists<T>(string docName)
        {
            var collection = GetCollection<T>();
            return collection.Exists(x => x.Id == docName);
        }
    }
}