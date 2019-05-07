using System.Collections.Generic;
using RavenBOT.Services.Database;

namespace RavenBOT.Services
{
    public class PrefixService
    {
        private IDatabase Store { get; }
        private PrefixInfo Info { get; }
        private string DocumentName { get; }
        private bool Developer { get; }
        private string DeveloperPrefix { get; }

        public PrefixService(IDatabase store, string defaultPrefix, bool developer = false, string developerPrefix = "dev.")
        {
            DocumentName = "PrefixSetup";
            Developer = developer;
            DeveloperPrefix = developerPrefix;
            Store = store;

            var doc = Store.Load<PrefixInfo>(DocumentName);
            if (doc == null)
            {
                doc = new PrefixInfo(defaultPrefix);
                store.Store(doc, DocumentName);
            }

            Info = doc;
        }

        public string GetPrefix(ulong guildId)
        {
            if (Developer)
            {
                return DeveloperPrefix;
            }

            return Info.GetPrefix(guildId);
        }

        public void SetPrefix(ulong guildId, string prefix)
        {
            Info.SetPrefix(guildId, prefix);
            Store.Store(Info, DocumentName);
        }

        public class PrefixInfo
        {
            private Dictionary<ulong, string> Prefixes { get; } = new Dictionary<ulong, string>();
            private string DefaultPrefix { get; }

            public PrefixInfo(string defaultPrefix)
            {
                DefaultPrefix = defaultPrefix;
            }

            public void SetPrefix(ulong guildId, string prefix)
            {
                

                if (Prefixes.ContainsKey(guildId))
                {
                    if (prefix == null)
                    {
                        Prefixes.Remove(guildId);
                    }
                    else
                    {
                       Prefixes[guildId] = prefix; 
                    }
                }
                else
                {
                    Prefixes.TryAdd(guildId, prefix);
                }
            }

            public string GetPrefix(ulong guildId)
            {
                if (Prefixes.ContainsKey(guildId))
                {
                    return Prefixes[guildId];
                }

                return DefaultPrefix;
            }
        }
    }
}
