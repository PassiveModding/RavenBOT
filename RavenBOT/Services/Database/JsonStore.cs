using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RavenBOT.Services.Database
{
    //This is a poor implementation of a 'database' and should only be used as a last resort or for testing purposes
    public class JsonStore : IDatabase
    {
        public string StorageDirectory = Path.Combine(AppContext.BaseDirectory, "JsonStore");

        public JsonStore()
        {
            if (!Directory.Exists(StorageDirectory))
            {
                Directory.CreateDirectory(StorageDirectory);
            }
        }

        private string DocumentName(string name)
        {
            return Path.Combine(StorageDirectory, $"{name}.json");
        }

        public void Store<T>(T document, string name = null)
        {
            var docString = JsonConvert.SerializeObject(document);
            if (name != null)
            {
                File.WriteAllText(DocumentName(name), docString);
            }
            else
            {
                //Find the amount of document stored in the folder.
                var docCount = Directory.GetFiles(StorageDirectory, "*.json").Count();
                var docType = document.GetType().FullName;
                //Ensure there is NOT a document with the same name as the current one
                while (File.Exists(Path.Combine(StorageDirectory, $"{docType}{docCount}")))
                {
                    //Increment until there is a free name available.
                    docCount++;
                }

                //Write the value to the new file.
                File.WriteAllText(DocumentName($"{docType}{docCount}"), docString);
            }
        }

        public void StoreMany<T>(List<T> documents, Func<T, string> docName = null)
        {
            foreach (var document in documents)
            {
                Store(document, docName(document));
            }
        }

        public T Load<T>(string documentName)
        {
            var docName = DocumentName(documentName);
            if (File.Exists(docName))
            {
                var file = File.ReadAllText(docName);
                return JsonConvert.DeserializeObject<T>(file);
            }

            return default(T);
        }

        public List<T> Query<T>()
        {
            var list = new List<T>();
            foreach (var fileName in Directory.GetFiles(StorageDirectory, "*.json"))
            {
                try
                {
                    var text = File.ReadAllText(fileName);
                    var classType = JsonConvert.DeserializeObject<T>(text);
                    list.Add(classType);
                }
                catch
                {
                    //
                }
            }

            return list;
        }
        
        public void RemoveDocument<T>(T document)
        {
            var docText = JsonConvert.SerializeObject(document);
            foreach (var fileName in Directory.GetFiles(StorageDirectory, "*.json"))
            {
                try
                {
                    var text = File.ReadAllText(fileName);
                    if (docText.Equals(text))
                    {
                        File.Delete(fileName);
                    }
                }
                catch
                {
                    //
                }
            }
        }

        public void Remove<T>(string documentName)
        {
            if (File.Exists(DocumentName(documentName)))
            {
                File.Delete(DocumentName(documentName));
            }
        }
    }
}
