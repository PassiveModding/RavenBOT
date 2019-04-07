using System.Collections.Generic;

namespace RavenBOT.Models
{
    public class DatabaseConfig
    {
        public string DatabaseName { get; set; }

        public List<string> DatabaseUrls { get; set; } = new List<string>();

        public string pathToCertificate { get; set; } = null;

        public string GraphiteUrl { get; set; } = null;
    }
}
