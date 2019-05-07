using System;
using System.Collections.Generic;

namespace RavenBOT.Services
{
    public interface IDatabase
    {
        void Store<T>(T document, string name = null);
        void StoreMany<T>(List<T> documents, Func<T, string> docName = null);
        T Load<T>(string documentName);
        List<T> Query<T>();
    }
}
