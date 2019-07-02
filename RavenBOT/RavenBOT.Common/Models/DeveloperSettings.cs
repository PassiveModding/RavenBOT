using System.Collections.Generic;

namespace RavenBOT.Common
{
    public class DeveloperSettings : IServiceable
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