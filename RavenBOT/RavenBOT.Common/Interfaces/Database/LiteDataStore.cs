using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;

namespace RavenBOT.Common.Interfaces.Database
{
    /*
        Some notes about serializing documents with LiteDB:
        https://stackoverflow.com/questions/39877806/get-litedb-to-inform-us-when-a-property-cannot-be-set

        Classes must be public with a public parameterless constructor
        Properties must be public
        Properties can be read-only or read/write
        The class must have an Id property, Id property or any property with [BsonId] attribute or mapped by fluent api.
        A property can be decorated with [BsonIgnore] to not be mapped to a document field
        A property can be decorated with [BsonField] to customize the name of the document field
        No circular references are allowed
        Max depth of 20 inner classes
        Class fields are not converted to document

     */

    /// <summary>
    /// This class is used as a `hack` for storing any class in LiteDB by adding an external ID property to the class prior to serailization
    /// This is implemented such that it should never need to be interacted with outside of the LiteDataStore class
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        public static string DatabaseFolder = Path.Combine(AppContext.BaseDirectory, "LiteDB");

        public static string DatabasePath = Path.Combine(DatabaseFolder, "LiteDB.db");

        public LiteDatabase Database { get; }

        private static readonly Object locker = new Object();
        public LiteDataStore()
        {
            //TODO: Ignore if path is specified
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
            }
            Database = new LiteDatabase("Filename=" + DatabasePath + "; utc=true;");
        }

        public void Store<T>(T document, string name = null)
        {
            var collection = GetCollection<T>();
            StoreInCollection(collection, document, name);
        }

        public void StoreInCollection<T>(LiteCollection<BaseEntity<T>> collection, T document, string name = null)
        {
            lock(locker)
            {
                collection.Upsert(new BaseEntity<T>(document, name));
            }
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

        public T Load<T>(string documentName) where T : class
        {
            lock(locker)
            {
                var collection = GetCollection<T>();
                try
                {
                    var doc = collection.FindOne(x => x.Id == documentName);
                    if (doc == null) return null;
                    return doc.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return default(T);
                }
            }
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> queryFunc)
        {
            var collection = GetCollection<T>();
            var func = queryFunc.Compile();
            var all = collection.FindAll();
            var filtered = all.Where(x => func(x.Value));
            return filtered.Select(x => x.Value);
        }

        public IEnumerable<T> Query<T>()
        {
            var collection = GetCollection<T>();
            var all = collection.FindAll();
            return all.Select(x => x.Value);
        }

        public void RemoveDocument<T>(T document)
        {
            lock(locker)
            {
                var collection = GetCollection<T>();
                collection.Delete(x => x.Equals(document));
            }
        }

        private LiteCollection<BaseEntity<T>> GetCollection<T>()
        {
            var collection = Database.GetCollection<BaseEntity<T>>(typeof(T).Name);
            return collection;
        }

        public void Remove<T>(string documentName)
        {
            lock(locker)
            {
                var collection = GetCollection<T>();
                collection.Delete(x => x.Id == documentName);
            }
        }

        public void RemoveManyDocuments<T>(List<T> documents)
        {
            lock(locker)
            {
                var collection = GetCollection<T>();
                collection.Delete(x => documents.Contains(x.Value));
            }
        }

        public void RemoveMany<T>(List<string> docNames)
        {
            lock(locker)
            {
                var collection = GetCollection<T>();
                foreach (var name in docNames)
                {
                    collection.Delete(x => x.Id == name);
                }
            }
        }

        public bool Exists<T>(string docName)
        {
            var collection = GetCollection<T>();
            return collection.Exists(x => x.Id == docName);
        }

        public bool Any<T>(Expression<Func<T, bool>> queryFunc)
        {
            lock (locker)
            {     
                //TODO: Check that this is valid           
                var collection = GetCollection<T>();
                var func = queryFunc.Compile();
                return collection.Exists(x => func(x.Value));
            }
        }
    }
}