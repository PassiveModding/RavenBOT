using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RavenBOT.Common.Interfaces
{
    public interface IDatabase
    {
        void Store<T>(T document, string name = null);
        void StoreMany<T>(List<T> documents, Func<T, string> docName = null);
        T Load<T>(string documentName);
        IEnumerable<T> Query<T>(Expression<Func<T, bool>> queryFunc);
        IEnumerable<T> Query<T>();
        void RemoveDocument<T>(T document);
        void Remove<T>(string documentName);
        void RemoveManyDocuments<T>(List<T> documents);
        void RemoveMany<T>(List<string> docNames);
        bool Exists<T>(string docName);
    }
}