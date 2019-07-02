using System.Collections.Generic;

namespace RavenBOT.Common
{
    public class PrefixService
    {
        private IDatabase Store { get; }
        private PrefixInfo Info { get; }
        private string DocumentName { get; }

        public string DefaultPrefix { get; }

        public PrefixService(IDatabase store, string defaultPrefix)
        {
            DocumentName = "PrefixSetup";
            Store = store;
            DefaultPrefix = defaultPrefix;

            var doc = Store.Load<PrefixInfo>(DocumentName);
            if (doc == null)
            {
                doc = new PrefixInfo();
                store.Store(doc, DocumentName);
            }

            Info = doc;
        }

        public string GetPrefix(ulong guildId)
        {
            return Info.GetPrefix(guildId) ?? DefaultPrefix;
        }

        public void SetPrefix(ulong guildId, string prefix)
        {
            Info.SetPrefix(guildId, prefix);
            Store.Store(Info, DocumentName);
        }

        public class PrefixInfo
        {
            private Dictionary<ulong, string> Prefixes { get; } = new Dictionary<ulong, string>();

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

                return null;
            }
        }
    }
}