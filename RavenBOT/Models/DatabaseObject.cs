namespace RavenBOT.Models
{
    using System.IO;

    /// <summary>
    /// The object used for initializing and using our database
    /// </summary>
    public class DatabaseObject
    {
        /// <summary>
        /// Gets or sets Time period for full backup
        /// </summary>
        public string FullBackup { get; set; } = "0 */6 * * *";

        /// <summary>
        /// Gets or sets Time period for incremental backup
        /// </summary>
        public string IncrementalBackup { get; set; } = "0 2 * * *";

        /// <summary>
        /// Gets or sets a value indicating whether the config is created.
        /// </summary>
        public bool IsConfigCreated { get; set; }

        /// <summary>
        /// Gets or sets The name.
        /// </summary>
        public string Name { get; set; } = "RavenBOT";

        /// <summary>
        /// Gets or sets The url.
        /// </summary>
        public string URL { get; set; } = "http://127.0.0.1:8080";

        /// <summary>
        /// The backup folder.
        /// </summary>
        public string BackupFolder => Directory.CreateDirectory("Backup").FullName;
    }
}