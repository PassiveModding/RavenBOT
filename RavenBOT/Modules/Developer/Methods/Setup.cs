using System.Collections.Generic;
using Raven.Client.Documents;

namespace RavenBOT.Modules.Developer.Methods
{
    public class Setup
    {
        public IDocumentStore Store { get; }

        public Setup(IDocumentStore store)
        {
            Store = store;
        }

        public Settings GetDeveloperSettings()
        {
            using (var session = Store.OpenSession())
            {
                var settings = session.Load<Settings>("DeveloperSettings");
                if (settings == null)
                {
                    settings = new Settings();
                    session.Store(settings, "DeveloperSettings");
                    session.SaveChanges();
                }

                return settings;
            }
        }

        public void SetDeveloperSettings(Settings newSettings)
        {
            using (var session = Store.OpenSession())
            {
                session.Store(newSettings, "DeveloperSettings");
                session.SaveChanges();
            }
        }

        public class Settings
        {
            public Settings()
            {
                SkippableHelpPreconditions = new List<string>();
            }

            public List<string> SkippableHelpPreconditions { get; set; }
        }
    }
}
