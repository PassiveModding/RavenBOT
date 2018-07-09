namespace RavenBOT.Models
{
    using RavenBOT.Handlers;

    /// <summary>
    /// The guild model.
    /// </summary>
    public class GuildModel
    {
        /// <summary>
        /// Gets or sets The Server ID
        /// </summary>
        public ulong ID { get; set; }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public GuildSettings Settings { get; set; } = new GuildSettings();

        /// <summary>
        /// Saves the GuildModel
        /// </summary>
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, ID.ToString());
                session.SaveChanges();
            }
        }

        /// <summary>
        /// The guild settings.
        /// </summary>
        public class GuildSettings
        {
        }
    }
}