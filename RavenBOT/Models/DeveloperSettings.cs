using System.Collections.Generic;
using RavenBOT.Services;
using RavenBOT.Services.Database;

namespace RavenBOT.Models
{
    public class DeveloperSettings
    {
        public IDatabase Store { get; }

        public DeveloperSettings(IDatabase store)
        {
            Store = store;
        }

        public Settings GetDeveloperSettings()
        {
            var settings = Store.Load<Settings>("DeveloperSettings");
            if (settings == null)
            {
                settings = new Settings();
                Store.Store(settings, "DeveloperSettings");
            }

            return settings;
        }

        public void SetDeveloperSettings(Settings newSettings)
        {
            Store.Store(newSettings, "DeveloperSettings");
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
